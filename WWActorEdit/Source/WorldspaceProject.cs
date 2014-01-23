﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms.VisualStyles;
using WWActorEdit.Kazari;
using WWActorEdit.Kazari.DZB;
using WWActorEdit.Kazari.DZx;
using WWActorEdit.Kazari.J3Dx;
using WWActorEdit.Source.FileFormats;

namespace WWActorEdit.Source
{
    /// <summary>
    /// A Worldspace Project refers to a collection of Stages and Rooms. This class acts as meta-data about
    /// a project the user is working on as nothing in here will get compiled into the actual archive. However
    /// it allows us to easily ekep track of which stages/rooms the user has open and their associated file
    /// structure.
    /// </summary>
    public class WorldspaceProject
    {
        //This refers to the Stage these files belong to. Null if no stage
        public ZArchive Stage { get; private set; }

        //This is a list of currently loaded Rooms. Returns a list of length zero if no rooms are loaded.
        public List<ZArchive> Rooms { get; private set; }

        //This is the name of Worlspace Project, sans .wrkDir extension. Max 8 chars.
        public string Name;

        public WorldspaceProject()
        {
            Rooms = new List<ZArchive>();
            Stage = null;
            Name = String.Empty;
        }

        public List<ZArchive> GetAllArchives()
        {
            List<ZArchive> archive = new List<ZArchive>(Rooms);
            if(Stage != null)
                archive.Add(Stage);

            return archive;
        }

        /// <summary>
        /// This will create a new WorldspaceProject from an existing working directory.
        /// </summary>
        /// <param name="dirFilePath">A filepath that ends in ".wrkDir" that is the root folder of the project.</param>
        public void LoadFromDirectory(string dirFilePath)
        {
            //Name (sans .wrkDir)
            string wrkDirName = new DirectoryInfo(dirFilePath).Name;
            Name = wrkDirName.Substring(0, wrkDirName.LastIndexOf(".wrkDir"));
            


            //We're going to scan for folders in this directory and construct ZArchives out of their contents.
            string[] subFolders = Directory.GetDirectories(dirFilePath);

            //We'll generate a ZArchive for each subfolder and load the ZArchive with their contents
            foreach (string folder in subFolders)
            {
                ZArchive arc = new ZArchive();
                arc.LoadFromDirectory(folder);
               
                //Check to see if this is a stage (name starts with "Stage") or a Room ("Room")
                string folderName = new DirectoryInfo(folder).Name;
                arc.Name = folderName;

                if (folderName.ToLower().StartsWith("stage"))
                {
                    Console.WriteLine("Loaded Stage for " + Name);
                    Stage = arc;
                    arc.RoomNumber = -1;
                }
                else
                {
                    Console.WriteLine("Loading \"" + folderName + "\" as Room for " + Name);
                    Rooms.Add(arc);

                    // In a 'Stage', there is data that is indexed by Room number. The actual rooms don't store
                    // this data internally, it is only by file name. So we're going to strip apart the filename
                    // to get the room number. If we can't get the room from the filename (ie: user has renamed
                    // archive) then we'll just ask them.
                    int roomNumber = 0;
                    
                    //If it starts with "Room" then it's (probably) a Windwaker Archive.
                    if (folderName.Substring(0, 4).ToLower() == "room")
                    {
                        //Use Regex here to grab what is between "Room" and ".arc", since it goes up to "Room23.arc"
                        string[] numbers = Regex.Split(folderName, @"\D+");
                        string trimmedNumbers = String.Join("", numbers);
                        trimmedNumbers = trimmedNumbers.Trim();

                        roomNumber = int.Parse(trimmedNumbers);
                    }
                    //If it starts with R ("Rxx_00, xx being Room Number"), it's Twlight Princess
                    else if (folderName.Substring(0, 1).ToLower() == "r")
                    {
                        //I *think* these follow the Rxx_00 pattern, where xx is the room number. _00 can change, xx might be 1 or 3, who knows!

                        //We're going to use RegEx here to make sure we only grab what is between R and _00 which could be multipl.e
                        string[] numbers = Regex.Split(folderName.Substring(0, folderName.Length - 6), @"\D+");
                        string trimmedNumbers = String.Join("", numbers);
                        trimmedNumbers = trimmedNumbers.Trim();

                        roomNumber = int.Parse(trimmedNumbers);
                    }
                    else
                    {
                        InvalidRoomNumber popup = new InvalidRoomNumber();
                        popup.DescriptionLabel.Text =
                            "Failed to determine room number from file name." + Environment.NewLine + "Expected: Room<x>.arc or R<xx>_00, got: " +
                            folderName;
                        popup.ShowDialog(MainForm.ActiveForm);

                        roomNumber = (int)popup.roomNumberSelector.Value;
                    }

                    arc.RoomNumber = roomNumber;
                }
            }

        }
    }

    /// <summary>
    /// All file formats inside an archive should derive ultimately from BaseArchiveFile,
    /// because BaseArchiveFile has really important meta data (ie: FileName/FolderName)
    /// that I kept out of the interface so that each class wouldn't have to implement it.
    /// However, the Generic ZArchive::GetFileByType<T> requires us to use an interface
    /// for casting.
    /// </summary>
    public interface IArchiveFile
    {
        /* DON'T DERIVE ARCHIVE FILE FORMATS FROM THIS INTERFACE, DERIVE FROM BaseArchiveFile INSTEAD */

        void Load(byte[] data);
        void Save(BinaryWriter stream);
    }

    /// <summary>
    /// Well, this was originally going an IArchiveFile interface that all of the
    /// archive file types (dzs, dzb, etc.) derived from. However, the desire to
    /// include a filename field (so we can save back out to the disk) has pushed
    /// us to use an abstract class with a field!
    /// 
    /// All ArchiveFile types (dzb, dzr, bdl, etc.) should derive from this base
    /// class as it provides information for loading/saving to the 
    /// WorldspaceProject / wrkDir. 
    /// </summary>
    public abstract class BaseArchiveFile : IArchiveFile
    {
        //What folder does this get saved into (dzb, dzr, etc.)
        public string FolderName;

        //What the file name was (room.dzb, etc.)
        public string FileName;


        public abstract void Load(byte[] data);
        public abstract void Save(BinaryWriter stream);
    }

    public class ZArchive
    {
        //This is a list of all loaded files from the Archive.
        private readonly List<IArchiveFile> _archiveFiles;

        //This is the name of the Archive, ie: "Room0" "Stage", etc.
        public string Name;

        //If this is a Room, the Room number, -1 if stage.
        public int RoomNumber;

        public ZArchive()
        {
            _archiveFiles = new List<IArchiveFile>();
            Name = "Unnamed";
        }

        public void AddFileToArchive(IArchiveFile file)
        {
            _archiveFiles.Add(file);
        }

        public List<BaseArchiveFile> GetAllFiles()
        {
            return _archiveFiles.Cast<BaseArchiveFile>().ToList();
        }

        /// <summary>
        /// Invokes the Save() interface on each ArchiveFile. Generates the required
        /// folders and saves out each individual file.
        /// </summary>
        /// <param name="archiveRootFolder">Folder inside the WrkDir to save to, ie: "C:\...\MiniHyo\Room0\"</param>
        public void Save(string archiveRootFolder)
        {
            foreach (IArchiveFile archiveFile in _archiveFiles)
            {
                BaseArchiveFile file = (BaseArchiveFile) archiveFile;
                if(file == null)
                    continue;

                //Create the sub-folder
                string subFolder = Path.Combine(archiveRootFolder, file.FolderName);
                Directory.CreateDirectory(subFolder);

                //Open the file for Read/Write Access
                FileStream fs = new FileStream(Path.Combine(subFolder, file.FileName), FileMode.Create);
                try
                {
                    BinaryWriter bw = new BinaryWriter(fs);
                    file.Save(bw);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error saving file " + file.FileName + " to " + subFolder + "! Error: " + ex);
                }
            }
            
        }
        
        /// <summary>
        /// Pass this a Room<x> folder or a Stage folder directory! This will look for specific subfolders 
        /// (bdl, btk, dzb, dzr, dzs, dat, etc.) and load each file within them as appropriate.
        /// </summary>
        /// <param name="directory">Absolute file path to a folder containing a bdl/btk/etc. folder(s)</param>
        public void LoadFromDirectory(string directory)
        {
            //Get all of the sub folders (bdl, btk, etc.)
            string[] subFolders = Directory.GetDirectories(directory);


            foreach (string folder in subFolders)
            {
                //Then grab all of the files that are inside this folder and we'll load each one.
                string[] subFiles = Directory.GetFiles(folder);

                foreach (string filePath in subFiles)
                {
                    BinaryReader br = new BinaryReader(File.OpenRead(filePath));
                    //try
                    //{
                        BaseArchiveFile file;

                        byte[] fileData = br.ReadBytes((int) br.BaseStream.Length);
                        switch ((new DirectoryInfo(folder).Name).ToLower())
                        {
                            /* 3D Model Formats */
                            case "bmd":
                            case "bdl":
                            case "bck":
                            case "brk":
                            case "btk":
                                file = new J3Dx();
                                break;

                            /* Map Collision Format */
                            case "dzb":
                                file = new DZB();
                                break;

                            /* Room and Stage Entity Data */
                            case "dzr":
                            case "dzs":
                                //Apparently Nintendo likes to mis-categorize files sometimes and put the wrong
                                //file format inside the wrong folder! We'll name-check dzr and dzs before loading
                                //them as they have fixed names (Room.*)
                                if(filePath.EndsWith(".dzr") || filePath.EndsWith(".dzs"))
                                    file = new ZeldaData();
                                else
                                    file = new GenericData();
                                break;

                            default:
                                Console.WriteLine("Unknown folder " + folder +
                                                  " found. Creating GenericData holder for it!");
                                file = new GenericData();
                                break;
                        }

                        file.Load(fileData);
                        file.FileName = Path.GetFileName(filePath);
                        file.FolderName = new DirectoryInfo(folder).Name;

                        //Now that we've created the appropriate file (and hopefully mapped them all out!) we'll just stick
                        //it in our list of loaded files. They can later be gotten with the templated getter!
                        _archiveFiles.Add(file);
                        br.Close();
                    //}
                    //catch (Exception ex)
                    //{
                        //Console.WriteLine("Error opening file " + filePath + " for reading. Error Message: " + ex);
                    //}
                }
            }
        }

        /// <summary>
        /// Returns the first file IArchiveFile derived class or null if no files of that exists.
        /// Use GetAllFilesByType<T> if there may be multiple of a file (as is the case with models)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetFileByType<T>()
        {
            foreach (IArchiveFile file in _archiveFiles)
            {
                if (file is T)
                    return (T) file;
            }

            return default(T);
        }


        public List<T> GetAllFilesByType<T>()
        {
            List<T> returnList = new List<T>();
            foreach (IArchiveFile file in _archiveFiles)
            {
                if (file is T)
                    returnList.Add((T) file);
            }

            return returnList;
        }
    }
}
