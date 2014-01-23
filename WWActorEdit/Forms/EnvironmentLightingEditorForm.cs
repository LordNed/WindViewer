using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Blue.Windows;
using OpenTK;
using WWActorEdit.Kazari;
using WWActorEdit.Kazari.DZx;
using WWActorEdit.Source;

namespace WWActorEdit.Forms
{
    public partial class EnvironmentLightingEditorForm : Form
    {
        //Set by the MainForm when it opens this Popup
        private MainForm _mainForm;

        private ZeldaData _data;

        //These are references to the currently selected EnvR/Color/Etc. chunks. They are
        //used to populate the UI with the values of the selected index. When the index
        //is changed the data is still kept (inside the _data tree), but these references
        //will change.
        private EnvRChunk _envrChunk;
        private ColoChunk _coloChunk;
        private PaleChunk _paleChunk;
        private VirtChunk _virtChunk;

        //Used for "dockable" WinForms
        private StickyWindow _stickyWindow;

        public EnvironmentLightingEditorForm(MainForm parent)
        {
            InitializeComponent();

            _mainForm = parent;

            _stickyWindow = new StickyWindow(this);
        }

        private void OnSelectedEntityFileChanged(ZeldaData entFile)
        {
            //Clear the old dropdowns.
            EnvRDropdown.Items.Clear();
            ColorDropdown.Items.Clear();
            PaleDropdown.Items.Clear();
            VirtDropdown.Items.Clear();

            ResetEnvrGroupBox();
            ResetColoGroupBox();
            ResetPaleGroupBox();
            ResetVirtGroupBox();

            _envrChunk = null;
            _coloChunk = null;
            _paleChunk = null;
            _virtChunk = null;
            _data = null;

            //First we're going to grab the chunks, populate the dropdowns if they exist.
            List<EnvRChunk> envrChunks = entFile.GetAllChunks<EnvRChunk>();
            for (int i = 0; i < envrChunks.Count; i++)
                EnvRDropdown.Items.Add("EnvR [" + i + "]");
            List<ColoChunk> coloChunks = entFile.GetAllChunks<ColoChunk>();
            for (int i = 0; i < coloChunks.Count; i++)
                ColorDropdown.Items.Add("Colo [" + i + "]");
            List<PaleChunk> paleChunks = entFile.GetAllChunks<PaleChunk>();
            for (int i = 0; i < paleChunks.Count; i++)
                PaleDropdown.Items.Add("Pale [" + i + "]");
            List<VirtChunk> virtChunks = entFile.GetAllChunks<VirtChunk>();
            for (int i = 0; i < virtChunks.Count; i++)
                VirtDropdown.Items.Add("Virt [" + i + "]");

            _data = entFile;
            EnvRDropdown.SelectedIndex = envrChunks.Count > 0 ? 0 : -1;
            ColorDropdown.SelectedIndex = coloChunks.Count > 0 ? 0 : -1;
            PaleDropdown.SelectedIndex = paleChunks.Count > 0 ? 0 : -1;
            VirtDropdown.SelectedIndex = virtChunks.Count > 0 ? 0 : -1;
        }

        /// <summary>
        /// This will update the values within the Environment GroupBox to point to whatever the
        /// current _envrChunk's values are. 
        /// </summary>
        private void UpdateEnvrGroupBox()
        {
            //If they have Type A selected we populate the same UI elements but with different data...
            if (EnvRTypeA.Checked)
            {
                EnvRClearSkiesIndex.Value = _envrChunk.ClearColorIndexA;
                EnvRRainingIndex.Value = _envrChunk.RainingColorIndexA;
                EnvRSnowingIndex.Value = _envrChunk.SnowingColorIndexA;
                EnvRUnknownIndex.Value = _envrChunk.UnknownColorIndexA;
            }
            else
            {
                EnvRClearSkiesIndex.Value = _envrChunk.ClearColorIndexB;
                EnvRRainingIndex.Value = _envrChunk.RainingColorIndexB;
                EnvRSnowingIndex.Value = _envrChunk.SnowingColorIndexB;
                EnvRUnknownIndex.Value = _envrChunk.UnknownColorIndexB;
            }
        }

        private void ResetEnvrGroupBox()
        {
            EnvRClearSkiesIndex.Value = 0;
            EnvRRainingIndex.Value = 0;
            EnvRSnowingIndex.Value = 0;
            EnvRUnknownIndex.Value = 0;
        }

        /// <summary>
        /// This will update all of the values within the Color GroupBox to point to whatever the
        /// //current _coloChunk's values are.
        /// </summary>
        private void UpdateColoGroupBox()
        {
            ColoDawnIndex.Value = _coloChunk.DawnIndex;
            ColoMorningIndex.Value = _coloChunk.MorningIndex;
            ColoNoonIndex.Value = _coloChunk.NoonIndex;
            ColoAfternoonIndex.Value = _coloChunk.AfternoonIndex;
            ColoDuskIndex.Value = _coloChunk.DuskIndex;
            ColoNightIndex.Value = _coloChunk.NightIndex;
        }

        private void ResetColoGroupBox()
        {
            ColoDawnIndex.Value = 0;
            ColoMorningIndex.Value = 0;
            ColoNoonIndex.Value = 0;
            ColoAfternoonIndex.Value = 0;
            ColoDuskIndex.Value = 0;
            ColoNightIndex.Value = 0;
        }

        /// <summary>
        /// This updates all of the values within the Pale Groupbox to point to whatever the current
        /// _paleChunk's values are.
        /// </summary>
        private void UpdatePaleGroupBox()
        {
            PaleActorAmbientColor.BackColor = SetPaleColorBoxColor(_paleChunk.ActorAmbient);
            PaleShadowColor.BackColor = SetPaleColorBoxColor(_paleChunk.ShadowColor);
            PaleRoomFillColor.BackColor = SetPaleColorBoxColor(_paleChunk.RoomFillColor);
            PaleRoomAmbientColor.BackColor = SetPaleColorBoxColor(_paleChunk.RoomAmbient);
            PaleWaveColor.BackColor = SetPaleColorBoxColor(_paleChunk.WaveColor);
            PaleOceanColor.BackColor = SetPaleColorBoxColor(_paleChunk.OceanColor);
            PaleUnknown1Color.BackColor = SetPaleColorBoxColor(_paleChunk.UnknownColor1);
            PaleUnknown2Color.BackColor = SetPaleColorBoxColor(_paleChunk.UnknownColor2);
            PaleDoorwayColor.BackColor = SetPaleColorBoxColor(_paleChunk.DoorwayColor);
            PaleUnknown3Color.BackColor = SetPaleColorBoxColor(_paleChunk.UnknownColor3);
            PaleFogColor.BackColor = SetPaleColorBoxColor(_paleChunk.FogColor);

            PaleVirtIndex.Value = _paleChunk.VirtIndex;
            PaleOceanFadeIntoColor.BackColor = SetPaleColorBoxColor(_paleChunk.OceanFadeInto);
            PaleOceanFadeAlpha.Value = _paleChunk.OceanFadeInto.A;

            PaleShoreFadeIntoColor.BackColor = SetPaleColorBoxColor(_paleChunk.ShoreFadeInto);
            PaleShoreFadeAlpha.Value = _paleChunk.ShoreFadeInto.A;
        }

        private void ResetPaleGroupBox()
        {
            PaleActorAmbientColor.BackColor = SystemColors.ActiveCaption;
            PaleShadowColor.BackColor = SystemColors.ActiveCaption;
            PaleRoomFillColor.BackColor = SystemColors.ActiveCaption;
            PaleRoomAmbientColor.BackColor = SystemColors.ActiveCaption;
            PaleWaveColor.BackColor = SystemColors.ActiveCaption;
            PaleOceanColor.BackColor = SystemColors.ActiveCaption;
            PaleUnknown1Color.BackColor = SystemColors.ActiveCaption;
            PaleUnknown2Color.BackColor = SystemColors.ActiveCaption;
            PaleDoorwayColor.BackColor = SystemColors.ActiveCaption;
            PaleUnknown3Color.BackColor = SystemColors.ActiveCaption;
            PaleFogColor.BackColor = SystemColors.ActiveCaption;

            PaleVirtIndex.Value = 0;
            PaleOceanFadeIntoColor.BackColor = SystemColors.ActiveCaption;
            PaleOceanFadeAlpha.Value = 0;

            PaleShoreFadeIntoColor.BackColor = SystemColors.ActiveCaption;
            PaleShoreFadeAlpha.Value = 0;
        }

        private void UpdateVirtGroupBox()
        {
            VirtHorizonCloudColor.BackColor = SetPaleColorBoxColor(_virtChunk.HorizonCloudColor);
            VirtUnknown1Index.Value = _virtChunk.HorizonCloudColor.A;

            VirtCenterCloudColor.BackColor = SetPaleColorBoxColor(_virtChunk.CenterCloudColor);
            VirtUnknown2Index.Value = _virtChunk.CenterCloudColor.A;

            VirtCenterSkyColor.BackColor = SetPaleColorBoxColor(_virtChunk.CenterSkyColor);
            VirtHorizonColor.BackColor = SetPaleColorBoxColor(_virtChunk.HorizonColor);
            VirtSkyFadeToColor.BackColor = SetPaleColorBoxColor(_virtChunk.SkyFadeTo);
        }

        private void ResetVirtGroupBox()
        {
            VirtHorizonCloudColor.BackColor = SystemColors.ActiveCaption;
            VirtUnknown1Index.Value = 0;

            VirtCenterCloudColor.BackColor = SystemColors.ActiveCaption;
            VirtUnknown2Index.Value = 0;

            VirtCenterSkyColor.BackColor = SystemColors.ActiveCaption;
            VirtHorizonColor.BackColor = SystemColors.ActiveCaption;
            VirtSkyFadeToColor.BackColor = SystemColors.ActiveCaption;
        }

        /// <summary>
        /// Add an event listener for when the user changes which Entity file is selected in the
        /// tree view.
        /// </summary>
        private void EnvironmentLightingEditorForm_Load(object sender, EventArgs e)
        {
            MainForm.SelectedEntityDataFileChanged += OnSelectedEntityFileChanged;

            //If there is already a map loaded when the editor opens then this will be set.
            ZeldaData entData = MainForm.SelectedData;
            if (entData != null)
                OnSelectedEntityFileChanged(entData);
        }

        /// <summary>
        /// Called when the user changes the EnvR type from A to B or back. Need to update all of the Values because
        /// we're sharing controls for types A and B.
        /// </summary>
        private void EnvRType_CheckedChanged(object sender, EventArgs e)
        {
            UpdateEnvrGroupBox();
        }

        /// <summary>
        /// Called when the user changes the EnvR dropdown index. We'll need to update all of the Values to point to
        /// the new EnvR element.
        /// </summary>
        private void EnvRDropdown_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (EnvRDropdown.SelectedIndex < 0)
                return;

            _envrChunk = _data.GetAllChunks<EnvRChunk>()[EnvRDropdown.SelectedIndex];
            UpdateEnvrGroupBox();
        }

        /// <summary>
        /// Called when the user changes the Color dropdown index.
        /// </summary>
        private void ColorDropdown_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (ColorDropdown.SelectedIndex < 0)
                return;

            _coloChunk = _data.GetAllChunks<ColoChunk>()[ColorDropdown.SelectedIndex];
            UpdateColoGroupBox();
        }

        private void PaleDropdown_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (PaleDropdown.SelectedIndex < 0)
                return;

            _paleChunk = _data.GetAllChunks<PaleChunk>()[PaleDropdown.SelectedIndex];
            UpdatePaleGroupBox();
        }

        private void VirtDropdown_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (VirtDropdown.SelectedIndex < 0)
                return;

            _virtChunk = _data.GetAllChunks<VirtChunk>()[VirtDropdown.SelectedIndex];
            UpdateVirtGroupBox();
        }

        /// <summary>
        /// Called when ANY of the color fields in Pale are clicked on.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PaleColorField_Click(object sender, EventArgs e)
        {
            //Set the color in the Color Picker to what it currently is
            //And then pause the app till we get a new color.
            PictureBox outputBox = (PictureBox) sender;

            colorPickerDialog.Color = outputBox.BackColor;
            colorPickerDialog.ShowDialog(this);

            outputBox.BackColor = colorPickerDialog.Color;

            //Update whoever generated the event.
            if(sender == PaleActorAmbientColor)
                _paleChunk.ActorAmbient = SetPaleMemoryColor(PaleActorAmbientColor);
            if(sender == PaleRoomFillColor)
                _paleChunk.RoomFillColor = SetPaleMemoryColor(PaleRoomFillColor);
            if(sender == PaleRoomAmbientColor)
                _paleChunk.RoomAmbient = SetPaleMemoryColor(PaleRoomAmbientColor);
            if(sender == PaleWaveColor)
                _paleChunk.WaveColor = SetPaleMemoryColor(PaleWaveColor);
            if (sender == PaleUnknown1Color)
                _paleChunk.UnknownColor1 = SetPaleMemoryColor(PaleUnknown1Color);
            if (sender == PaleUnknown2Color)
                _paleChunk.UnknownColor2 = SetPaleMemoryColor(PaleUnknown2Color);
            if(sender == PaleOceanColor)
                _paleChunk.OceanColor = SetPaleMemoryColor(PaleOceanColor);
            if (sender == PaleUnknown3Color)
                _paleChunk.UnknownColor3 = SetPaleMemoryColor(PaleUnknown3Color);
            if(sender == PaleDoorwayColor)
                _paleChunk.DoorwayColor = SetPaleMemoryColor(PaleDoorwayColor);
            if(sender == PaleFogColor)
                _paleChunk.FogColor = SetPaleMemoryColor(PaleFogColor);

            if (sender == PaleOceanFadeIntoColor)
            {
                ByteColorAlpha OceanFadeInto = new ByteColorAlpha(SetPaleMemoryColor(PaleOceanFadeIntoColor));
                OceanFadeInto.A = (byte)PaleOceanFadeAlpha.Value;
                _paleChunk.OceanFadeInto = OceanFadeInto;
            }

            if (sender == PaleShoreFadeIntoColor)
            {
                ByteColorAlpha ShoreFadeInto = new ByteColorAlpha(SetPaleMemoryColor(PaleShoreFadeIntoColor));
                ShoreFadeInto.A = (byte) PaleShoreFadeAlpha.Value;
                _paleChunk.ShoreFadeInto = ShoreFadeInto;
            }
        }

        private void VirtColorField_Click(object sender, EventArgs e)
        {
            PictureBox outputBox = (PictureBox)sender;

            colorPickerDialog.Color = outputBox.BackColor;
            colorPickerDialog.ShowDialog(this);

            outputBox.BackColor = colorPickerDialog.Color;

            if (sender == VirtHorizonCloudColor)
            {
                ByteColorAlpha HorizonCloud = new ByteColorAlpha(SetPaleMemoryColor(VirtHorizonCloudColor));
                HorizonCloud.A = (byte) VirtUnknown1Index.Value;
                _virtChunk.HorizonCloudColor = HorizonCloud;
            }

            if (sender == VirtCenterCloudColor)
            {
                ByteColorAlpha CenterCloud = new ByteColorAlpha(SetPaleMemoryColor(VirtCenterCloudColor));
                CenterCloud.A = (byte)VirtUnknown2Index.Value;
                _virtChunk.CenterCloudColor = CenterCloud;
            }
           
            if(sender == VirtCenterSkyColor)
                _virtChunk.CenterSkyColor = SetPaleMemoryColor(VirtCenterSkyColor);
            if(sender == VirtHorizonColor)
                _virtChunk.HorizonColor = SetPaleMemoryColor(VirtHorizonColor);
            if(sender == VirtSkyFadeToColor)
                _virtChunk.SkyFadeTo = SetPaleMemoryColor(VirtSkyFadeToColor);
        }

        private Color SetPaleColorBoxColor(ByteColor color)
        {
            Color newColor = Color.FromArgb(color.R, color.G, color.B);
            return newColor;
        }

        private Color SetPaleColorBoxColor(ByteColorAlpha color)
        {
            Color newColor = Color.FromArgb(255, color.R, color.G, color.B);
            return newColor;
        }

        private ByteColor SetPaleMemoryColor(PictureBox source)
        {
            ByteColor c = new ByteColor();
            c.R = source.BackColor.R;
            c.G = source.BackColor.G;
            c.B = source.BackColor.B;

            return c;
        }

        /// <summary>
        /// Called when any of the Indexes change in the EnvRGroup.
        /// </summary>
        private void EnvRGroupBoxIndex_ValueChanged(object sender, EventArgs e)
        {
            //Going to just copy all of their values back into the _envRChunk,
            //because I haven't come up with a better way yet!
            //If they have Type A selected we populate the same UI elements but with different data...
            if (EnvRTypeA.Checked)
            {
                if(sender == EnvRClearSkiesIndex)
                    _envrChunk.ClearColorIndexA = (byte) EnvRClearSkiesIndex.Value;
                if(sender == EnvRRainingIndex)
                    _envrChunk.RainingColorIndexA = (byte) EnvRRainingIndex.Value;
                if(sender == EnvRSnowingIndex)
                    _envrChunk.SnowingColorIndexA = (byte) EnvRSnowingIndex.Value;
                if(sender == EnvRUnknownIndex)
                    _envrChunk.UnknownColorIndexA = (byte) EnvRUnknownIndex.Value;
            }
            else
            {
                if (sender == EnvRClearSkiesIndex)
                    _envrChunk.ClearColorIndexB = (byte)EnvRClearSkiesIndex.Value;
                if (sender == EnvRRainingIndex)
                    _envrChunk.RainingColorIndexB = (byte)EnvRRainingIndex.Value;
                if (sender == EnvRSnowingIndex)
                    _envrChunk.SnowingColorIndexB = (byte)EnvRSnowingIndex.Value;
                if (sender == EnvRUnknownIndex)
                    _envrChunk.UnknownColorIndexB = (byte)EnvRUnknownIndex.Value;
            }
        }

        /// <summary>
        /// Called when any of the indexes in the Pale group change.
        /// </summary>
        private void PaleIndex_ValueChanged(object sender, EventArgs e)
        {
            if(sender == PaleVirtIndex)
                _paleChunk.VirtIndex = (byte) PaleVirtIndex.Value;
            if (sender == PaleOceanFadeAlpha)
                _paleChunk.OceanFadeInto.A = (byte) PaleOceanFadeAlpha.Value;
            if (sender == PaleShoreFadeAlpha)
                _paleChunk.ShoreFadeInto.A = (byte) PaleShoreFadeAlpha.Value;
        }

        /// <summary>
        /// Called when either of the Unknown groups in Virt change.
        /// </summary>
        private void VirtUnknownIndex_ValueChanged(object sender, EventArgs e)
        {
            if(sender == VirtUnknown1Index)
                _virtChunk.HorizonCloudColor.A = (byte) VirtUnknown1Index.Value;
            if(sender == VirtUnknown2Index)
                _virtChunk.CenterCloudColor.A = (byte)VirtUnknown2Index.Value;
        }

        /// <summary>
        /// Called when anything in the Color GroupBox change.
        /// </summary>
        private void ColoGroupBoxIndex_ValueChanged(object sender, EventArgs e)
        {
            if(sender == ColoDawnIndex)
                _coloChunk.DawnIndex = (byte) ColoDawnIndex.Value;
            if(sender == ColoMorningIndex)
                _coloChunk.MorningIndex = (byte) ColoMorningIndex.Value;
            if(sender == ColoNoonIndex)
                _coloChunk.NoonIndex = (byte) ColoNoonIndex.Value;
            if(sender == ColoAfternoonIndex)
                _coloChunk.AfternoonIndex = (byte) ColoAfternoonIndex.Value;
            if(sender == ColoDuskIndex)
                _coloChunk.DuskIndex = (byte) ColoDuskIndex.Value;
            if(sender == ColoNightIndex)
                _coloChunk.NightIndex = (byte) ColoNightIndex.Value;
        }

        private void saveButton_Click(object sender, EventArgs e)
        {
        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            Close();
        }
        
    }
}
