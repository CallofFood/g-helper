﻿using GHelper.Peripherals.Mouse;
using GHelper.UI;

namespace GHelper
{
    public partial class AsusMouseSettings : RForm
    {
        private static Dictionary<LightingMode, string> lightingModeNames = new Dictionary<LightingMode, string>()
        {
            { LightingMode.Static,Properties.Strings.AuraStatic},
            { LightingMode.Breathing, Properties.Strings.AuraBreathe},
            { LightingMode.ColorCycle, Properties.Strings.AuraColorCycle},
            { LightingMode.Rainbow, Properties.Strings.AuraRainbow},
            { LightingMode.React, Properties.Strings.AuraReact},
            { LightingMode.Comet, Properties.Strings.AuraComet},
            { LightingMode.BatteryState, Properties.Strings.AuraBatteryState},
            { LightingMode.Off, Properties.Strings.MatrixOff},
        };
        private List<LightingMode> supportedLightingModes = new List<LightingMode>();

        private readonly AsusMouse mouse;
        private readonly RButton[] dpiButtons;

        private bool updateMouseDPI = true;

        public AsusMouseSettings(AsusMouse mouse)
        {
            this.mouse = mouse;
            InitializeComponent();

            dpiButtons = new RButton[] { buttonDPI1, buttonDPI2, buttonDPI3, buttonDPI4 };


            labelPollingRate.Text = Properties.Strings.PollingRate;
            labelLighting.Text = Properties.Strings.Lighting;
            labelEnergy.Text = Properties.Strings.EnergySettings;
            labelPerformance.Text = Properties.Strings.MousePerformance;
            checkBoxRandomColor.Text = Properties.Strings.AuraRandomColor;
            labelLowBatteryWarning.Text = Properties.Strings.MouseLowBatteryWarning;
            labelAutoPowerOff.Text = Properties.Strings.MouseAutoPowerOff;
            buttonSync.Text = Properties.Strings.MouseSynchronize;
            checkBoxAngleSnapping.Text = Properties.Strings.MouseAngleSnapping;
            labelLiftOffDistance.Text = Properties.Strings.MouseLiftOffDistance;
            labelChargingState.Text = "(" + Properties.Strings.Charging + ")";
            labelProfile.Text = Properties.Strings.Profile;

            InitTheme();

            this.Text = mouse.GetDisplayName();

            Shown += AsusMouseSettings_Shown;
            FormClosing += AsusMouseSettings_FormClosing;

            comboProfile.DropDownClosed += ComboProfile_DropDownClosed;

            sliderDPI.ValueChanged += SliderDPI_ValueChanged;
            numericUpDownCurrentDPI.ValueChanged += NumericUpDownCurrentDPI_ValueChanged;
            sliderDPI.MouseUp += SliderDPI_MouseUp;
            sliderDPI.MouseDown += SliderDPI_MouseDown;
            buttonDPIColor.Click += ButtonDPIColor_Click;
            buttonDPI1.Click += ButtonDPI_Click;
            buttonDPI2.Click += ButtonDPI_Click;
            buttonDPI3.Click += ButtonDPI_Click;
            buttonDPI4.Click += ButtonDPI_Click;

            comboBoxPollingRate.DropDownClosed += ComboBoxPollingRate_DropDownClosed;
            checkBoxAngleSnapping.CheckedChanged += CheckAngleSnapping_CheckedChanged;
            sliderAngleAdjustment.ValueChanged += SliderAngleAdjustment_ValueChanged;
            sliderAngleAdjustment.MouseUp += SliderAngleAdjustment_MouseUp;
            comboBoxLiftOffDistance.DropDownClosed += ComboBoxLiftOffDistance_DropDownClosed;

            buttonLightingColor.Click += ButtonLightingColor_Click;
            comboBoxLightingMode.DropDownClosed += ComboBoxLightingMode_DropDownClosed;
            sliderBrightness.MouseUp += SliderBrightness_MouseUp;
            comboBoxAnimationSpeed.DropDownClosed += ComboBoxAnimationSpeed_DropDownClosed;
            comboBoxAnimationDirection.DropDownClosed += ComboBoxAnimationDirection_DropDownClosed;
            checkBoxRandomColor.CheckedChanged += CheckBoxRandomColor_CheckedChanged;

            sliderLowBatteryWarning.ValueChanged += SliderLowBatteryWarning_ValueChanged;
            sliderLowBatteryWarning.MouseUp += SliderLowBatteryWarning_MouseUp;
            comboBoxAutoPowerOff.DropDownClosed += ComboBoxAutoPowerOff_DropDownClosed;

            InitMouseCapabilities();
            Logger.WriteLine(mouse.GetDisplayName() + " (GUI): Initialized capabilities. Synchronizing mouse data");
            RefreshMouseData();
        }

        private void AsusMouseSettings_FormClosing(object? sender, FormClosingEventArgs e)
        {
            mouse.BatteryUpdated -= Mouse_BatteryUpdated;
            mouse.Disconnect -= Mouse_Disconnect;
            mouse.MouseReadyChanged -= Mouse_MouseReadyChanged;
        }

        private void Mouse_MouseReadyChanged(object? sender, EventArgs e)
        {
            if (Disposing || IsDisposed)
            {
                return;
            }
            if (!mouse.IsDeviceReady)
            {
                this.Invoke(delegate
                {
                    Close();
                });
            }
        }

        private void Mouse_BatteryUpdated(object? sender, EventArgs e)
        {
            if (Disposing || IsDisposed)
            {
                return;
            }
            this.Invoke(delegate
            {
                VisualizeBatteryState();
            });

        }

        private void ComboProfile_DropDownClosed(object? sender, EventArgs e)
        {
            mouse.SetProfile(comboProfile.SelectedIndex);
            Task task = Task.Run((Action)RefreshMouseData);
        }

        private void ComboBoxPollingRate_DropDownClosed(object? sender, EventArgs e)
        {
            mouse.SetPollingRate(mouse.SupportedPollingrates()[comboBoxPollingRate.SelectedIndex]);
        }

        private void ButtonDPIColor_Click(object? sender, EventArgs e)
        {
            ColorDialog colorDlg = new ColorDialog
            {
                AllowFullOpen = true,
                Color = pictureDPIColor.BackColor
            };

            if (colorDlg.ShowDialog() == DialogResult.OK)
            {
                AsusMouseDPI dpi = mouse.DpiSettings[mouse.DpiProfile - 1];
                dpi.Color = colorDlg.Color;

                mouse.SetDPIForProfile(dpi, mouse.DpiProfile);

                VisualizeDPIButtons();
                VisualizeCurrentDPIProfile();
            }
        }

        private void ButtonDPI_Click(object? sender, EventArgs e)
        {
            int index = -1;

            for (int i = 0; i < dpiButtons.Length; ++i)
            {
                if (sender == dpiButtons[i])
                {
                    index = i;
                    break;
                }
            }

            if (index == -1)
            {
                //huh?
                return;
            }

            mouse.SetDPIProfile(index + 1);
            VisualizeDPIButtons();
            VisualizeCurrentDPIProfile();
        }


        private void CheckBoxRandomColor_CheckedChanged(object? sender, EventArgs e)
        {
            LightingSetting? ls = mouse.LightingSetting;
            ls.RandomColor = checkBoxRandomColor.Checked;

            mouse.SetLightingSetting(ls);
            VisusalizeLightingSettings();
        }

        private void ComboBoxAnimationDirection_DropDownClosed(object? sender, EventArgs e)
        {
            LightingSetting? ls = mouse.LightingSetting;
            ls.AnimationDirection = (AnimationDirection)comboBoxAnimationDirection.SelectedIndex;

            mouse.SetLightingSetting(ls);
            VisusalizeLightingSettings();
        }

        private void ComboBoxAnimationSpeed_DropDownClosed(object? sender, EventArgs e)
        {
            LightingSetting? ls = mouse.LightingSetting;
            ls.AnimationSpeed = (AnimationSpeed)comboBoxAnimationSpeed.SelectedIndex;

            mouse.SetLightingSetting(ls);
            VisusalizeLightingSettings();
        }

        private void SliderBrightness_MouseUp(object? sender, MouseEventArgs e)
        {
            LightingSetting? ls = mouse.LightingSetting;
            ls.Brightness = sliderBrightness.Value;

            mouse.SetLightingSetting(ls);
        }

        private void ComboBoxLightingMode_DropDownClosed(object? sender, EventArgs e)
        {
            if (!mouse.HasRGB())
            {
                return;
            }

            LightingMode lm = supportedLightingModes[comboBoxLightingMode.SelectedIndex];

            LightingSetting? ls = mouse.LightingSetting;
            ls.LightingMode = lm;

            mouse.SetLightingSetting(ls);
            VisusalizeLightingSettings();
        }

        private void ButtonLightingColor_Click(object? sender, EventArgs e)
        {
            ColorDialog colorDlg = new ColorDialog
            {
                AllowFullOpen = true,
                Color = pictureBoxLightingColor.BackColor
            };

            if (colorDlg.ShowDialog() == DialogResult.OK)
            {
                LightingSetting? ls = mouse.LightingSetting;
                ls.RGBColor = colorDlg.Color;

                mouse.SetLightingSetting(ls);
                VisusalizeLightingSettings();
            }
        }

        private void SliderLowBatteryWarning_ValueChanged(object? sender, EventArgs e)
        {
            labelLowBatteryWarningValue.Text = sliderLowBatteryWarning.Value.ToString() + "%";
        }

        private void SliderLowBatteryWarning_MouseUp(object? sender, MouseEventArgs e)
        {
            mouse.SetEnergySettings(sliderLowBatteryWarning.Value, mouse.PowerOffSetting);
        }


        private void ComboBoxAutoPowerOff_DropDownClosed(object? sender, EventArgs e)
        {
            object? obj = Enum.GetValues(typeof(PowerOffSetting)).GetValue(comboBoxAutoPowerOff.SelectedIndex);
            if (obj is null)
            {
                return;
            }
            PowerOffSetting pos = (PowerOffSetting)obj;


            mouse.SetEnergySettings(mouse.LowBatteryWarning, pos);
        }

        private void SliderAngleAdjustment_ValueChanged(object? sender, EventArgs e)
        {
            labelAngleAdjustmentValue.Text = sliderAngleAdjustment.Value.ToString() + "°";
        }

        private void SliderAngleAdjustment_MouseUp(object? sender, MouseEventArgs e)
        {
            mouse.SetAngleAdjustment((short)sliderAngleAdjustment.Value);
        }

        private void ComboBoxLiftOffDistance_DropDownClosed(object? sender, EventArgs e)
        {
            mouse.SetLiftOffDistance((LiftOffDistance)comboBoxLiftOffDistance.SelectedIndex);
        }

        private void CheckAngleSnapping_CheckedChanged(object? sender, EventArgs e)
        {
            mouse.SetAngleSnapping(checkBoxAngleSnapping.Checked);
            mouse.SetAngleAdjustment((short)sliderAngleAdjustment.Value);
        }

        private void SliderDPI_ValueChanged(object? sender, EventArgs e)
        {
            numericUpDownCurrentDPI.Value = sliderDPI.Value;
            UpdateMouseDPISettings();
        }

        private void NumericUpDownCurrentDPI_ValueChanged(object? sender, EventArgs e)
        {
            sliderDPI.Value = (int)numericUpDownCurrentDPI.Value;
        }

        private void SliderDPI_MouseDown(object? sender, MouseEventArgs e)
        {
            updateMouseDPI = false;
        }

        private void SliderDPI_MouseUp(object? sender, MouseEventArgs e)
        {
            updateMouseDPI = true;
            UpdateMouseDPISettings();
        }

        private void UpdateMouseDPISettings()
        {
            if (!updateMouseDPI)
            {
                return;
            }
            AsusMouseDPI dpi = mouse.DpiSettings[mouse.DpiProfile - 1];
            dpi.DPI = (uint)sliderDPI.Value;

            mouse.SetDPIForProfile(dpi, mouse.DpiProfile);

            VisualizeDPIButtons();
            VisualizeCurrentDPIProfile();
        }

        private void Mouse_Disconnect(object? sender, EventArgs e)
        {
            //Mouse disconnected. Bye bye.
            this.Invoke(delegate
            {
                if (Disposing || IsDisposed)
                {
                    return;
                }
                this.Close();
            });

        }

        private void RefreshMouseData()
        {
            mouse.SynchronizeDevice();

            Logger.WriteLine(mouse.GetDisplayName() + " (GUI): Mouse data synchronized");
            if (!mouse.IsDeviceReady)
            {
                Logger.WriteLine(mouse.GetDisplayName() + " (GUI): Mouse is not ready. Closing view.");
                this.Invoke(delegate
                {
                    this.Close();
                });
                return;
            }

            this.Invoke(delegate
            {
                VisualizeMouseSettings();
                VisualizeBatteryState();
            });
        }

        private void InitMouseCapabilities()
        {
            for (int i = 0; i < mouse.ProfileCount(); ++i)
            {
                String prf = Properties.Strings.Profile + " " + (i + 1);
                comboProfile.Items.Add(prf);
            }

            labelMinDPI.Text = mouse.MinDPI().ToString();
            labelMaxDPI.Text = mouse.MaxDPI().ToString();

            sliderDPI.Max = mouse.MaxDPI();
            sliderDPI.Min = mouse.MinDPI();

            numericUpDownCurrentDPI.Minimum = mouse.MinDPI();
            numericUpDownCurrentDPI.Maximum = mouse.MaxDPI();


            if (!mouse.HasDPIColors())
            {
                buttonDPIColor.Visible = false;
                pictureDPIColor.Visible = false;
                buttonDPI1.Image = ControlHelper.TintImage(Properties.Resources.lighting_dot_24, Color.Red);
                buttonDPI2.Image = ControlHelper.TintImage(Properties.Resources.lighting_dot_24, Color.Purple);
                buttonDPI3.Image = ControlHelper.TintImage(Properties.Resources.lighting_dot_24, Color.Blue);
                buttonDPI4.Image = ControlHelper.TintImage(Properties.Resources.lighting_dot_24, Color.Green);

                buttonDPI1.BorderColor = Color.Red;
                buttonDPI1.BorderColor = Color.Purple;
                buttonDPI1.BorderColor = Color.Blue;
                buttonDPI1.BorderColor = Color.Green;
            }

            if (mouse.CanSetPollingRate())
            {
                foreach (PollingRate pr in mouse.SupportedPollingrates())
                {
                    comboBoxPollingRate.Items.Add(mouse.PollingRateDisplayString(pr));
                }

            }
            else
            {
                comboBoxPollingRate.Visible = false;
                labelPollingRate.Visible = false;
            }

            if (!mouse.HasAngleSnapping())
            {
                checkBoxAngleSnapping.Visible = false;
            }

            if (!mouse.HasAngleTuning())
            {
                labelAngleAdjustmentValue.Visible = false;
                sliderAngleAdjustment.Visible = false;
            }

            if (mouse.HasLiftOffSetting())
            {
                comboBoxLiftOffDistance.Items.AddRange(new string[] {
                    Properties.Strings.Low,
                    Properties.Strings.High,
                });
            }
            else
            {
                comboBoxLiftOffDistance.Visible = false;
                labelLiftOffDistance.Visible = false;
            }

            if (mouse.DPIProfileCount() < 4)
            {
                for (int i = 3; i > mouse.DPIProfileCount() - 1; --i)
                {
                    dpiButtons[i].Visible = false;
                }
            }

            if (!mouse.HasBattery())
            {
                panelBatteryState.Visible = false;
            }

            if (mouse.HasAutoPowerOff())
            {
                comboBoxAutoPowerOff.Items.AddRange(new string[]{
                    " 1 "+ Properties.Strings.Minute,
                    " 2 "+ Properties.Strings.Minutes,
                    " 3 "+ Properties.Strings.Minutes,
                    " 5 "+ Properties.Strings.Minutes,
                    "10 "+ Properties.Strings.Minutes,
                     Properties.Strings.Never,
                });
            }

            if (!mouse.HasLowBatteryWarning())
            {
                labelLowBatteryWarning.Visible = false;
                labelLowBatteryWarningValue.Visible = false;
                sliderLowBatteryWarning.Visible = false;
            }

            if (!mouse.HasAutoPowerOff() && !mouse.HasLowBatteryWarning())
            {
                panelEnergy.Visible = false;
            }

            if (mouse.HasRGB())
            {
                foreach (LightingMode lm in Enum.GetValues(typeof(LightingMode)))
                {
                    if (mouse.IsLightingModeSupported(lm))
                    {
                        comboBoxLightingMode.Items.Add(lightingModeNames.GetValueOrDefault(lm));
                        supportedLightingModes.Add(lm);
                    }
                }

                comboBoxAnimationDirection.Items.AddRange(new string[] {
                    Properties.Strings.AuraClockwise,
                    Properties.Strings.AuraCounterClockwise,
                });

                comboBoxAnimationSpeed.Items.AddRange(new string[] {
                    Properties.Strings.AuraSlow,
                    Properties.Strings.AuraNormal,
                    Properties.Strings.AuraFast
                });
            }
            else
            {
                panelLighting.Visible = false;
            }
        }


        private void VisualizeMouseSettings()
        {
            comboProfile.SelectedIndex = mouse.Profile;

            VisualizeDPIButtons();
            VisualizeCurrentDPIProfile();
            VisusalizeLightingSettings();

            if (mouse.CanSetPollingRate())
            {
                int idx = mouse.PollingRateIndex(mouse.PollingRate);
                if (idx == -1)
                {
                    return;
                }
                comboBoxPollingRate.SelectedIndex = idx;
            }

            if (mouse.HasAngleSnapping())
            {
                checkBoxAngleSnapping.Checked = mouse.AngleSnapping;
            }

            if (mouse.HasAngleTuning())
            {
                sliderAngleAdjustment.Value = mouse.AngleAdjustmentDegrees;
            }

            if (mouse.HasAutoPowerOff())
            {
                if (mouse.PowerOffSetting == PowerOffSetting.Never)
                {
                    comboBoxAutoPowerOff.SelectedIndex = comboBoxAutoPowerOff.Items.Count - 1;
                }
                else
                {
                    comboBoxAutoPowerOff.SelectedIndex = (int)mouse.PowerOffSetting;
                }
            }

            if (mouse.HasLowBatteryWarning())
            {
                sliderLowBatteryWarning.Value = mouse.LowBatteryWarning;
            }

            if (mouse.HasLiftOffSetting())
            {
                comboBoxLiftOffDistance.SelectedIndex = (int)mouse.LiftOffDistance;
            }
        }

        private void VisualizeBatteryState()
        {
            if (!mouse.HasBattery())
            {
                return;
            }

            labelBatteryState.Text = mouse.Battery + "%";
            labelChargingState.Visible = mouse.Charging;

            if (mouse.Charging)
            {
                pictureBoxBatteryState.BackgroundImage = ControlHelper.TintImage(Properties.Resources.icons8_ladende_batterie_48, foreMain);
            }
            else
            {
                pictureBoxBatteryState.BackgroundImage = ControlHelper.TintImage(Properties.Resources.icons8_batterie_voll_geladen_48, foreMain);
            }
        }

        private void VisusalizeLightingSettings()
        {
            if (!mouse.HasRGB())
            {
                return;
            }

            LightingSetting? ls = mouse.LightingSetting;

            if (ls is null)
            {
                //Lighting settings not loaded?
                return;
            }

            sliderBrightness.Value = ls.Brightness;

            checkBoxRandomColor.Visible = mouse.SupportsRandomColor(ls.LightingMode);

            pictureBoxLightingColor.Visible = mouse.SupportsColorSetting(ls.LightingMode);
            buttonLightingColor.Visible = mouse.SupportsColorSetting(ls.LightingMode);

            comboBoxAnimationSpeed.Visible = mouse.SupportsAnimationSpeed(ls.LightingMode);
            labelAnimationSpeed.Visible = mouse.SupportsAnimationSpeed(ls.LightingMode);
            comboBoxAnimationDirection.Visible = mouse.SupportsAnimationDirection(ls.LightingMode);
            labelAnimationDirection.Visible = mouse.SupportsAnimationDirection(ls.LightingMode);

            comboBoxLightingMode.SelectedIndex = supportedLightingModes.IndexOf(ls.LightingMode);

            if (mouse.SupportsRandomColor(ls.LightingMode))
            {
                checkBoxRandomColor.Checked = ls.RandomColor;
                buttonLightingColor.Visible = !ls.RandomColor;
            }

            if (ls.RandomColor && mouse.SupportsRandomColor(ls.LightingMode))
                pictureBoxLightingColor.BackColor = Color.Transparent;
            else
                pictureBoxLightingColor.BackColor = ls.RGBColor;


            comboBoxAnimationSpeed.SelectedIndex = (((int)ls.AnimationSpeed) - 5) / 2;
            comboBoxAnimationDirection.SelectedIndex = (int)ls.AnimationDirection;
        }


        private void VisualizeDPIButtons()
        {
            for (int i = 0; i < mouse.DPIProfileCount() && i < 4; ++i)
            {
                AsusMouseDPI dpi = mouse.DpiSettings[i];
                if (dpi is null)
                {
                    continue;
                }
                if (mouse.HasDPIColors())
                {
                    dpiButtons[i].Image = ControlHelper.TintImage(Properties.Resources.lighting_dot_24, dpi.Color);
                    dpiButtons[i].BorderColor = dpi.Color;
                }
                dpiButtons[i].Activated = (mouse.DpiProfile - 1) == i;
                dpiButtons[i].Text = "DPI " + (i + 1) + "\n" + dpi.DPI;
            }
        }


        private void VisualizeCurrentDPIProfile()
        {
            AsusMouseDPI dpi = mouse.DpiSettings[mouse.DpiProfile - 1];
            sliderDPI.Value = (int)dpi.DPI;
            pictureDPIColor.BackColor = dpi.Color;
        }

        private void AsusMouseSettings_Shown(object? sender, EventArgs e)
        {

            if (Height > Program.settingsForm.Height)
            {
                Top = Program.settingsForm.Top + Program.settingsForm.Height - Height;
            }
            else
            {
                Top = Program.settingsForm.Top;
            }

            Left = Program.settingsForm.Left - Width - 5;


            mouse.Disconnect += Mouse_Disconnect;
            mouse.BatteryUpdated += Mouse_BatteryUpdated;
            mouse.MouseReadyChanged += Mouse_MouseReadyChanged;
        }

        private void ButtonSync_Click(object sender, EventArgs e)
        {
            Task task = Task.Run((Action)RefreshMouseData);
        }
    }
}
