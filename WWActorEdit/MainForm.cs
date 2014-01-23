using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Globalization;
using System.Windows.Forms.VisualStyles;
using Blue.Windows;
using FolderSelect;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using WWActorEdit.Forms;
using WWActorEdit.Kazari;
using WWActorEdit.Kazari.DZx;
using WWActorEdit.Kazari.DZB;
using WWActorEdit.Kazari.J3Dx;
using WWActorEdit.Source;
using WWActorEdit.Source.FileFormats;

namespace WWActorEdit
{
    public partial class MainForm : Form
    {
        //List of current loaded WorldspaceProjects (see WorldspaceProjects.cs for more information)
        private readonly List<WorldspaceProject> _loadedWorldspaceProjects;

        /* EVENTS */
        public static event Action WorldspaceProjectListModified;  //Fired when a WorldspaceProject is loaded or unloaded
        public static event Action<ZeldaData> SelectedEntityDataFileChanged; //Fired when the currently selected Room/Stage Entity Data changes in the FileBrowser Treeview.

        /* CACHED THINGS */ 
        public static ZeldaData SelectedData { get; private set; } //Currently selected DZS/DZR in Tree View, otherwise Null.
         

        /* MISC */
        private bool _glContextLoaded; //Has the GL Control been loaded? Used to prevent rendering before GL is Initialized.
        private StickyWindow _stickyWindow; //Used for "dockable" WinForms

        public MainForm()
        {
            //Initialize the WinForm
            InitializeComponent();

            _stickyWindow = new StickyWindow(this);
            _loadedWorldspaceProjects = new List<WorldspaceProject>();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            WorldspaceProjectListModified += RebuildFileBrowserTreeview;
            SelectedEntityDataFileChanged += RebuildEntityListTreeview;
        }

        #region GLControl
        void Application_Idle(object sender, EventArgs e)
        {
            while (glControl.IsIdle == true)
            {
                RenderFrame();
            }
        }

        void glControl_Load(object sender, EventArgs e)
        {
            Application.Idle += new EventHandler(Application_Idle);

            Helpers.Enable3DRendering(new SizeF(glControl.Width, glControl.Height));

            _glContextLoaded = true;
        }

        void glControl_Paint(object sender, PaintEventArgs e)
        {
            RenderFrame();
        }

        void glControl_Resize(object sender, EventArgs e)
        {
            if (_glContextLoaded == false) return;

            Helpers.Enable3DRendering(new SizeF(glControl.Width, glControl.Height));
            glControl.Invalidate();
        }

        void glControl_KeyDown(object sender, KeyEventArgs e)
        {
            Helpers.Camera.KeysDown[e.KeyValue] = true;
        }

        void glControl_KeyUp(object sender, KeyEventArgs e)
        {
            Helpers.Camera.KeysDown[e.KeyValue] = false;
        }

        void glControl_MouseDown(object sender, MouseEventArgs e)
        {
            switch (e.Button)
            {
                case MouseButtons.Left:
                    Helpers.Camera.Mouse.LDown = true;
                    break;
                case MouseButtons.Right:
                    Helpers.Camera.Mouse.RDown = true;
                    break;
                case MouseButtons.Middle:
                    Helpers.Camera.Mouse.MDown = true;
                    break;
            }

            Helpers.Camera.Mouse.Center = new Vector2(e.X, e.Y);

            if (Helpers.Camera.Mouse.LDown == true)
            {
                if (Helpers.Camera.Mouse.Center != Helpers.Camera.Mouse.Move)
                    Helpers.Camera.MouseMove(Helpers.Camera.Mouse.Move);
                else
                    Helpers.Camera.MouseCenter(Helpers.Camera.Mouse.Move);
            }
        }

        void glControl_MouseMove(object sender, MouseEventArgs e)
        {
            Helpers.Camera.Mouse.Move = new Vector2(e.X, e.Y);

            if (Helpers.Camera.Mouse.LDown == true)
            {
                if (Helpers.Camera.Mouse.Center != Helpers.Camera.Mouse.Move)
                    Helpers.Camera.MouseMove(Helpers.Camera.Mouse.Move);
                else
                    Helpers.Camera.MouseCenter(Helpers.Camera.Mouse.Move);
            }
        }

        void glControl_MouseUp(object sender, MouseEventArgs e)
        {
            switch (e.Button)
            {
                case MouseButtons.Left:
                    Helpers.Camera.Mouse.LDown = false;
                    break;
                case MouseButtons.Right:
                    Helpers.Camera.Mouse.RDown = false;
                    break;
                case MouseButtons.Middle:
                    Helpers.Camera.Mouse.MDown = false;
                    break;
            }
        }

        #endregion

        /// <summary>
        /// Instead of faking a Paint event inside the Application.Idle we'll just put
        /// the drawing into its own function and call it in both Application.Idle
        /// and in the Paint event of the GL control.
        /// </summary>
        private void RenderFrame()
        {
            if (_glContextLoaded == false) return;

            GL.ClearColor(Color.DodgerBlue);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            Helpers.Enable3DRendering(new SizeF(glControl.Width, glControl.Height));

            Helpers.Camera.Position();


            GL.Scale(0.005f, 0.005f, 0.005f);

            /* Models */
            foreach (WorldspaceProject worldspaceProject in _loadedWorldspaceProjects)
            {
                if (renderModelsToolStripMenuItem.Checked == true)
                {
                    foreach (ZArchive room in worldspaceProject.Rooms)
                    {
                        GL.PushMatrix();
                        //GetGlobalTranslation(A);
                        //GetGlobalRotation(A);

                        foreach (J3Dx M in room.GetAllFilesByType<J3Dx>())
                        {
                            /* Got model translation from Stage? (ex. rooms in sea) */
                            /*if (A.GlobalTranslation != Vector3.Zero || A.GlobalRotation != 0)
                            {
                                //Perform translation
                                GL.Translate(A.GlobalTranslation);
                                GL.Rotate(A.GlobalRotation, 0, 1, 0);
                            }*/
                            M.Render();
                        }

                        GL.PopMatrix();
                    }
      
                }

                // Collision
                if (renderCollisionToolStripMenuItem.Checked)
                {
                    foreach (ZArchive room in worldspaceProject.Rooms)
                    {
                        DZB D = room.GetFileByType<DZB>();
                        if (D != null)
                        {
                            D.Render();
                        }
                    }
                }
                // Actors, 1st pass 
                /*if (renderRoomActorsToolStripMenuItem.Checked == true)
                {
                   foreach (ZArchive room in worldspaceProject.Rooms)
                    {

                        if (A.DZRs != null) foreach (DZx D in A.DZRs) D.Render();
                        if (A.DZSs != null) foreach (DZx D in A.DZSs) D.Render();
                    }
                }
                if (renderStageActorsToolStripMenuItem.Checked == true && Stage != null)
                {
                    if (Stage.DZRs != null) foreach (DZx D in Stage.DZRs) D.Render();
                    if (Stage.DZSs != null) foreach (DZx D in Stage.DZSs) D.Render();
                }

                // Actors, 2nd pass 
                if (renderRoomActorsToolStripMenuItem.Checked == true)
                {
                    foreach (ZeldaArc A in Rooms)
                    {
                        if (A.DZRs != null) foreach (DZx D in A.DZRs) D.Render();
                        if (A.DZSs != null) foreach (DZx D in A.DZSs) D.Render();
                    }
                }
                if (renderStageActorsToolStripMenuItem.Checked == true && Stage != null)
                {
                    if (Stage.DZRs != null) foreach (DZx D in Stage.DZRs) D.Render();
                    if (Stage.DZSs != null) foreach (DZx D in Stage.DZSs) D.Render();
                }*/

            }

            Helpers.Camera.KeyUpdate();
            glControl.SwapBuffers();
        }

        #region Toolstrip Callbacks
        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.ExitThread();
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Version appVersion = Assembly.GetExecutingAssembly().GetName().Version;
            DateTime buildDate = new DateTime(2000, 1, 1).AddDays(appVersion.Build).AddSeconds(appVersion.Revision * 2);


            MessageBox.Show(
                Application.ProductName + " - Upgraded in 2014 by LordNed" + Environment.NewLine + Environment.NewLine +
                "Original written by xDaniel. Improvements by Pho, Abahbob and Sage of Mirrors." + Environment.NewLine +
                "RARC, Yaz0 and J3dx/BMD documentation by thakis, DZB, DZR, DZS documentation by" + Environment.NewLine +
                "Sage of Mirrors, Twili, fkualol, xdaniel, etc. Built on the backs of those who came before us." +
            Environment.NewLine + Environment.NewLine + "[Build: " + buildDate.ToString(CultureInfo.InvariantCulture) + "]", "About",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void environmentLightingEditorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            EnvironmentLightingEditorForm popup = new EnvironmentLightingEditorForm(this);
            popup.Show(this);
        }
        private void exitEditorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ExitEditor popup = new ExitEditor(this);
            popup.Show(this);
        }
        private void spawnEditorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SpawnpointEditor popup = new SpawnpointEditor(this);
            popup.Show(this);
        }

        /// <summary>
        /// Open the link to our Wiki which has more information about file formats, their usages, etc.
        /// Launches the default web browser on the users computer.
        /// </summary>
        private void wikiToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start(@"https://github.com/pho/WindViewer/wiki/");
        }

        /// <summary>
        /// Opens a Utility for converting Big-Endian floats from Hexidecimal to Float and back.
        /// </summary>
        private void floatConverterToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FloatConverter popup = new FloatConverter();
            popup.Show(this);
        }

        /// <summary>
        /// The "New from Archive..." is effectively the same as the old "Open Archive" feature.
        /// It will extract the selected Archive to the Working Directory and then invoke the
        /// same loading function as the "File->Open Worldspace Dir" option which is the actual
        /// loading routines used by the program.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void newFromArchiveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string[] filePaths = Helpers.ShowOpenFileDialog("Wind Waker Archives (*.arc; *.rarc)|*.arc; *.rarc|All Files (*.*)|*.*", true);

            //If they hit cancel it'll return an empty string.
            if (filePaths[0] == string.Empty)
                return;

            string workDir = CreateWorkingDirFromArchive(filePaths);
            if (workDir == string.Empty)
                return;

            //Now that we've extracted the files into the Working Dir (subdir), we'll invoke our regular
            //old "Open Project" type routine. Super clean!
            OpenFileFromWorkingDir(workDir);
        }

        /// <summary>
        /// This is the main "Open" file loading routine. It takes a workdir (directory ending in
        /// .wrkDir) that contains a Room<x> or Stage folders and loads them into a WorldspaceProject
        /// which is then stored in our list of loaded WorldspaceProjects.
        /// </summary>
        /// <param name="workDir"></param>
        private void OpenFileFromWorkingDir(string workDir)
        {
            //Iterate through the sub folders (dzb, dzr, bdl, etc.) and construct an appropriate data
            //structure for each one out of it. Then stick them all in a WorldspaceProject and save that
            //into our list of open projects. Then we can operate out of the WorldspaceProject references
            //and save and stuff.

            WorldspaceProject worldProj = new WorldspaceProject();
            worldProj.LoadFromDirectory(workDir);
            _loadedWorldspaceProjects.Add(worldProj);

            if (WorldspaceProjectListModified != null)
                WorldspaceProjectListModified();

        }

        /// <summary>
        /// Callback handler for opening an existing project.
        /// </summary>
        private void openWorldspaceDirToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //This is a crappy version of the thing but I can't find the WinForm someone made that replicates
            //the OpenFileDialog but for folders instead... Sorry!
            FolderSelectDialog ofd = new FolderSelectDialog();
            ofd.Title = "Navigate to a folder that ends in .wrkDir";

            string workingDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), Application.ProductName);
            ofd.InitialDirectory = workingDir;

            if (ofd.ShowDialog(this.Handle)) ;
            {
                //Ensure that the selected directory ends in ".wrkDir". If it doesn't, I don't want to figure out what happens.
                if (ofd.FileName.EndsWith(".wrkDir"))
                {
                    OpenFileFromWorkingDir(ofd.FileName);
                }
                else
                {
                    Console.WriteLine("Error: Select a folder that ends in .wrkDir!");
                }
            }
        }

        #endregion

        /// <summary>
        /// This creates a new "Working Dir" for a project (ie: "My Documents\WindViewer\MiniHyo"). It is the equivelent
        /// of setting up a project directory for new files. 
        /// </summary>
        /// <param name="archiveFilePaths">Archive to use as the base content to place in the WrkDir.</param>
        /// <returns></returns>
        private string CreateWorkingDirFromArchive(string[] archiveFilePaths)
        {
            //For each file selected we want to extract it to the working directory.
            string workingDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), Application.ProductName);

            //Next we're going to stop and retrieve the "Worldspace Dir" from the user (name of parent folder, ie:
            //"MiniHyo" or "DragonRoostIsland" or something fancy like that. We'll just have to ask the user!
            NewWorldspaceDialogue dialogue = new NewWorldspaceDialogue();
            DialogResult result = dialogue.ShowDialog();
            if (result == DialogResult.Cancel)
                return string.Empty;

            string worldspaceName = dialogue.dirName.Text;

            workingDir = Path.Combine(workingDir, worldspaceName + ".wrkDir");
            foreach (string filePath in archiveFilePaths)
            {
                //Don't like using the RARC class but it seems like it can do what I want for now...
                RARC arc = new RARC(filePath);

                //We're going to stick these inside a sub-folder inside the .wrkDir directory based on the Arc name (ie: "Room0.arc");
                string arcName = arc.Filename.Substring(0, arc.Filename.IndexOf('.'));
                string folderDir = Path.Combine(workingDir, arcName);

                foreach (RARC.FileNode node in arc.Root.ChildNodes)
                {
                    //Create the folder on disk to represent the folder in the Archive.
                    DirectoryInfo outputDir = Directory.CreateDirectory(Path.Combine(folderDir, node.NodeName));

                    //Now extract each of the files in the Archive into this folder.
                    foreach (RARC.FileEntry fileEntry in node.Files)
                    {
                        try
                        {
                            //Write the bytes to disk as a binary file and we'll have succesfully unpacked an archive, sweet!
                            FileStream fs = File.Create(Path.Combine(outputDir.FullName, fileEntry.FileName));
                            BinaryWriter bw = new BinaryWriter(fs);

                            bw.Write(fileEntry.GetFileData());
                            bw.Close();
                            fs.Close();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Error opening " + fileEntry.FileName + " for writing, error message: " +
                                              ex);
                        }

                    }
                }
            }

            return workingDir;
        }


        /// <summary>
        /// This rebuilds the File Browser Treeview (lower left) with a list of the 
        /// currently loaded WorldspaceProjects. It is invoked by the WorldspaceProjectListModified
        /// event and rebuilds the UI from scratch when you change loaded selections. Users will lose
        /// their selected Entity file, but oh well.
        /// </summary>
        private void RebuildFileBrowserTreeview()
        {
            //Wipe out any existing stuff.
            fileBrowserTV.Nodes.Clear();


            foreach (WorldspaceProject project in _loadedWorldspaceProjects)
            {
                //Create a Root node for this project
                TreeNode root = fileBrowserTV.Nodes.Add(project.Name, project.Name);
                foreach (ZArchive archive in project.GetAllArchives())
                {
                    TreeNode arcRoot = root.Nodes.Add(archive.Name, archive.Name);
                    foreach (BaseArchiveFile archiveFile in archive.GetAllFiles())
                    {
                        //Place the folder into the UI (this can be repeated so we only add if it doesn't exist)
                        TreeNode folderNode;
                        if (!arcRoot.Nodes.ContainsKey(archiveFile.FolderName))
                        {
                            folderNode = arcRoot.Nodes.Add(archiveFile.FolderName, archiveFile.FolderName);
                        }
                        else
                        {
                            TreeNode[] searchResults = arcRoot.Nodes.Find(archiveFile.FolderName, false);
                            folderNode = searchResults[0];
                        }

                        //Now the node for the folder will exist for sure, so we can add our file to it.
                        TreeNode fileName = folderNode.Nodes.Add(archiveFile.FileName);
                        fileName.Tag = archiveFile;

                        //We're going to select the Entity Data by default. We'll only select the first
                        //one which is probably a Room's.
                        if (archiveFile is ZeldaData && SelectedData == null)
                        {
                            //Generate the event once (manually)
                            if (SelectedEntityDataFileChanged != null)
                            {
                                SelectedData = (ZeldaData)archiveFile;
                                SelectedEntityDataFileChanged((ZeldaData)archiveFile);
                            }
                        }
                    }
                }
            }

            
            //Auto-expand the TreeView because it looks nice.
            fileBrowserTV.ExpandAll();
        }

        /// <summary>
        /// This is called when the user changes their selection in the File Browser. For now,
        /// we're just going to look to see what they selected, and if its an Entity file (dzs, dzr)
        /// then we'll update the curData TreeView.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void fileBrowserTV_AfterSelect(object sender, TreeViewEventArgs e)
        {
            //Lets hope they always pick the default name for Entity files...
            if (e.Node.Text.ToLower() == "room.dzr" || e.Node.Text.ToLower() == "stage.dzs")
            {
                //The user has selected an entity file. The reference to which entity file should
                //be stored in the node's tag, so with a little casting... magic!
                ZeldaData baseFile = (ZeldaData)e.Node.Tag;
                if (baseFile == null)
                {
                    Console.WriteLine("Error loading DZS/DZR for selected node. You should probably report this on our Issue Tracker!");
                    return;
                }

                //Now we're going to generate an event so that the floating WinForm editors can catch it too...
                if (SelectedEntityDataFileChanged != null)
                {
                    SelectedData = baseFile;
                    SelectedEntityDataFileChanged(baseFile);
                }
            }
        }


        /// <summary>
        /// This rebuilds the Entity List treeview (upper left) with a list of the data from 
        /// the currently selected ZeldaData entity file. It is invoked by the 
        /// SelectedEntityDataFileChanged event and rebuilds the Tree from scratch.
        /// </summary>
        private void RebuildEntityListTreeview(ZeldaData data)
        {
            //Wipe out any existing stuff
            curDataTV.Nodes.Clear();

            foreach (IChunkType chunk in data.GetAllChunks<IChunkType>())
            {
                TreeNode baseNode;
                if (!curDataTV.Nodes.ContainsKey(chunk.GetType().Name))
                {
                    baseNode = curDataTV.Nodes.Add(chunk.GetType().Name, chunk.GetType().Name);
                }
                else
                {
                    TreeNode[] nodes = curDataTV.Nodes.Find(chunk.GetType().Name, false);
                    baseNode = nodes[0];
                }

                baseNode.Nodes.Add("[" + baseNode.Nodes.Count + "] - " + chunk.GetType().Name);

            }

            //Expand everything
            curDataTV.ExpandAll();
        }
    }
}
