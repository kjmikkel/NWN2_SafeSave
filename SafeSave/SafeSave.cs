/* 
 * This file is part of SafeSave.
 * SafeSave is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * SafeSave is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser Public License for more details.
 * 
 * You should have received a copy of the GNU Lesser Public License
 * along with SafeSave. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.IO;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Numeric;
/*
 * NWN2Toolset.dll exposes all the functions we need to manupulate the toolkit.
 */
using NWN2Toolset.Plugins;
using NWN2Toolset;
using OEIShared.UI;
/*
 * Sandbar is the library thats used for the toolbar.
 * 
 * Windows also has a Toolbar object in System.Windows.Forms so make sure
 * you create the correct object when adding a toolbar to the toolkit.
 */
using TD.SandBar;
using System.ComponentModel;

namespace SafeSave
    {
    public class SafeSave : INWN2Plugin
        {
        private String PlugInName = "Safe Save";
        private MenuButtonItem m_cMenuItem;
        private Dictionary<string, ToolBarDef> AllToolbars;
        private SafeSaveProp preferences = new SafeSaveProp();

        #region Toolbar
        private enum ToolBarInitialPos
            {
            Top,
            Bottom,
            Left,
            Right
            };

        public void Startup(INWN2PluginHost cHost)
            {
            m_cMenuItem = cHost.GetMenuForPlugin(this);
            m_cMenuItem.Activate += new EventHandler(this.HandlePluginLaunch);
            // Add the following line of code to the end of the function
            buildToolbars();
            NWN2ToolsetMainForm.App.KeyPreview = true;
            NWN2ToolsetMainForm.App.KeyDown += new System.Windows.Forms.KeyEventHandler(NWN2BrushSaver);
            }

        public void NWN2BrushSaver(object sender, System.Windows.Forms.KeyEventArgs args)
            {
            if (preferences.Save.CompareTo(args.KeyData) == 0)
                {
                SaveInNewFile(null, null);
                }
            }

        private void HandlePluginLaunch(object sender, EventArgs e)
            {
            // Create the toolbar
            try
                {
                List<ToolBarDef> allToolbars = this.GetAllToolbars();
                for (int i = 0; i < allToolbars.Count; i++)
                    {
                    for (int j = 0; j < NWN2ToolsetMainForm.App.Controls.Count; j++)
                        {
                        if (NWN2ToolsetMainForm.App.Controls[j].GetType() == typeof(ToolBarContainer))
                            {
                            ToolBarContainer container = (ToolBarContainer)NWN2ToolsetMainForm.App.Controls[j];
                            if (container.Name == allToolbars[i].NWNToolsetDockName)
                                {
                                container.Controls.Add(allToolbars[i].toolBar);
                                break;
                                }
                            }
                        }
                    }

                }
            catch (Exception exception)
                {
                System.Windows.Forms.MessageBox.Show(exception.Message);
                }

            }

        private List<ToolBarDef> GetAllToolbars()
            {
            List<ToolBarDef> list = new List<ToolBarDef>();
            foreach (ToolBarDef def in this.AllToolbars.Values)
                {
                list.Add(def);
                }
            return list;
            }

        public struct ToolBarDef
            {
            public ToolBar toolBar;
            public string NWNToolsetDockName;
            };

        private void buildToolbars()
            {
            AllToolbars = new Dictionary<string, ToolBarDef>();
            CreateToolBar("SafeSave", ToolBarInitialPos.Top);
            ButtonItem item = new ButtonItem();
            item.Text = "Safe Save";
            item.ToolTipText = "Save your module in a new file";
            item.Activate += new EventHandler(SaveInNewFile);
            AddButtonToToolbar("SafeSave", item);
            }

        private string GetDockNameFromPosEnum(ToolBarInitialPos pos)
            {
            switch (pos)
                {
                case ToolBarInitialPos.Top:
                default:
                    return "topSandBarDock";
                case ToolBarInitialPos.Bottom:
                    return "bottomSandBarDock";
                case ToolBarInitialPos.Left:
                    return "leftSandBarDock";
                case ToolBarInitialPos.Right:
                    return "rightSandBarDock";
                }
            }

        private bool AddButtonToToolbar(string toolbarName, ButtonItem buttonToAdd)
            {
            if (this.AllToolbars.ContainsKey(toolbarName))
                {
                this.AllToolbars[toolbarName].toolBar.Items.Add(buttonToAdd);
                return true;
                }
            return false;
            }

        private void CreateToolBar(string name, ToolBarInitialPos initialPos)
            {
            ToolBar temp = new ToolBar();
            temp.Name = name;
            temp.Overflow = ToolBarOverflow.Hide;
            temp.AllowHorizontalDock = true;
            temp.AllowRightToLeft = true;
            temp.AllowVerticalDock = true;
            temp.Closable = false;
            temp.Movable = true;
            temp.Tearable = true;
            temp.DockLine = 2;

            ToolBarDef tbd = new ToolBarDef();
            tbd.NWNToolsetDockName = GetDockNameFromPosEnum(initialPos);
            tbd.toolBar = temp;

            AllToolbars.Add(name, tbd);
            }
        #endregion

        public void SaveInNewFile(object senter, EventArgs e)
            {
            NWN2Toolset.NWN2ToolsetMainForm.App.WaitForPanelsToSave();
            String file = NWN2ToolsetMainForm.App.Module.FileName;

            String path = Path.GetDirectoryName(file);
            String fileName = Path.GetFileNameWithoutExtension(file);
            String extension = Path.GetExtension(file);

            int date = 0;
            int digit;

            StringBuilder strBuilder = new StringBuilder();

            if (path != "")
                {
                strBuilder.Append(path);
                strBuilder.Append(@"\");
                }

            #region nameAndDigit
            if (extension == "")
                {
                date = System.DateTime.Today.ToUniversalTime().Millisecond;
                DirectoryInfo src = new DirectoryInfo(file);
                DirectoryInfo target = new DirectoryInfo(file + "temp" + date.ToString());

                CopyAll(src, target);
                }

            Regex number = new Regex(@"(\d+)$", RegexOptions.CultureInvariant);

            //if (int.TryParse(fileName[fileName.Length - 1].ToString(), out digit))
            Match match = number.Match(fileName);
            // System.Windows.Forms.MessageBox.Show(match.Success.ToString());
            if (match.Success)
                {
                digit = int.Parse(match.Groups[1].Value);
                //   System.Windows.Forms.MessageBox.Show(digit.ToString());
                int startOfDigit = match.Index;
                //   System.Windows.Forms.MessageBox.Show("start " + startOfDigit.ToString());

                // There is a number so we increment it and add the new 
                digit++;
                strBuilder.Append(fileName.Substring(0, startOfDigit));
                strBuilder.Append(digit.ToString());
                }
            else
                {
                // There is no number in front, so we just add a 1
                strBuilder.Append(fileName);
                strBuilder.Append("1");
                }
            #endregion

            strBuilder.Append(extension);
            NWN2ToolsetMainForm.App.Module.FileName = strBuilder.ToString();

            #region Saving
            if (NWN2Toolset.NWN2ToolsetMainForm.VersionControlManager.OnModuleSaving())
                {

                // Creating the ThreadSaveHelper
                ThreadedSaveHelper save = new ThreadedSaveHelper(NWN2Toolset.NWN2ToolsetMainForm.App,
                NWN2Toolset.NWN2ToolsetMainForm.App.Module.FileName,
                                                                 NWN2ToolsetMainForm.App.Module.LocationType);

                // Saving the module
                ThreadedProgressDialog progress = new ThreadedProgressDialog();
                progress.Text = "Saving";
                progress.Message = "Saving '" + NWN2Toolset.NWN2ToolsetMainForm.App.Module.FileName + "'";
                progress.WorkerThread = new ThreadedProgressDialog.WorkerThreadDelegate(save.Go);
                progress.ShowDialog(NWN2Toolset.NWN2ToolsetMainForm.App);
                }
            #endregion

            if (extension == "")
                {
                System.IO.Directory.Move(file + "temp" + date.ToString(), file);
                }
            }

        public static void CopyAll(DirectoryInfo source, DirectoryInfo target)
            {
            // Check if the target directory exists, if not, create it.
            if (Directory.Exists(target.FullName) == false)
                {
                Directory.CreateDirectory(target.FullName);
                }

            // Copy each file into it’s new directory.
            foreach (FileInfo fi in source.GetFiles())
                {
                fi.CopyTo(Path.Combine(target.ToString(), fi.Name), true);
                }

            // Copy each subdirectory using recursion.
            foreach (DirectoryInfo diSourceSubDir in source.GetDirectories())
                {
                DirectoryInfo nextTargetSubDir = target.CreateSubdirectory(diSourceSubDir.Name);
                CopyAll(diSourceSubDir, nextTargetSubDir);
                }
            }

        public void Load(INWN2PluginHost cHost)
            {
            }

        public void Shutdown(INWN2PluginHost cHost)
            {
            }


        public void Unload(INWN2PluginHost cHost)
            {
            }

        public MenuButtonItem PluginMenuItem
            {
            get
                {
                return m_cMenuItem;
                }
            }

        // Properties
        public string DisplayName
            {
            get
                {
                return PlugInName;
                }
            }

        public string MenuName
            {
            get
                {
                return PlugInName;
                }
            }

        public string Name
            {
            get
                {
                return PlugInName;
                }
            }

        public object Preferences
            {
            get
                {
                return preferences;
                }
            set
                {
                preferences = (SafeSaveProp)value;
                }
            }

        [Serializable]
        public class SafeSaveProp
            {
            System.Windows.Forms.Keys saveKey = System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.X;

            [Category("Shortcuts"), Description("Save"), Browsable(true), DefaultValue(typeof(System.Windows.Forms.Keys), "CTRL+X")]
            public System.Windows.Forms.Keys Save
                {
                get
                    {
                    return saveKey;
                    }
                set
                    {
                    saveKey = value;
                    }
                }
            }
        }
    }
