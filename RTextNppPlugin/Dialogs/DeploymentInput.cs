﻿using System;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Forms;

namespace NppPluginNET
{
    public partial class DeploymentInput : Form
    {
        public class RuntimeVersionItem
        {
            public string Version;
            public string DispalyTitle;
            public override string ToString()
            {
                return DispalyTitle;
            }
        }

        public RuntimeVersionItem SelectedVersion
        {
            get
            {
                if (versionsList.SelectedItem != null)
                    return (RuntimeVersionItem)versionsList.SelectedItem;
                else
                    return null;
            }
        }

        IEnumerable<RuntimeVersionItem> Versions
        {
            get { return versionsList.Items.Cast<RuntimeVersionItem>(); }
        }

        public DeploymentInput()
        {
            InitializeComponent();

            versionsList.Items.Add(new RuntimeVersionItem { Version = "v4.0.30319", DispalyTitle = "CLR v4.0 (.NET v4.0, v4.5)" });
            versionsList.Items.Add(new RuntimeVersionItem { Version = "v2.0.50727", DispalyTitle = "CLR v2.0 (.NET v2.0, v3.0, v3.5)" });

            versionsList.SelectedItem = Versions.Where(x => x.Version == Config.Instance.TargetVersion)
                                                .FirstOrDefault();

            if (versionsList.SelectedItem == null)
                versionsList.SelectedItem = Versions.First();

            asScript.Checked = Config.Instance.DistributeScriptAsScriptByDefault;
            asExe.Checked = !asScript.Checked;
            windowApp.Checked = Config.Instance.DistributeScriptAsWindowApp;
        }

        public bool AsScript
        {
            get { return asScript.Checked; }
        }

        public bool AsWindowApp
        {
            get { return windowApp.Checked; }
        }

        void okBtn_Click(object sender, EventArgs e)
        {
            Config.Instance.TargetVersion = SelectedVersion.Version;
            Config.Instance.DistributeScriptAsScriptByDefault = asScript.Checked;
            Config.Instance.DistributeScriptAsWindowApp = windowApp.Checked;
            Config.Instance.Save();
        }

        private void asExe_CheckedChanged(object sender, EventArgs e)
        {
            windowApp.Enabled = asExe.Checked;
        }
    }
}