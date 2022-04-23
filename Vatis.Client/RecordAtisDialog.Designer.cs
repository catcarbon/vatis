namespace Vatsim.Vatis.Client
{
    partial class RecordAtisDialog
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.btnRecord = new System.Windows.Forms.Button();
            this.btnListen = new System.Windows.Forms.Button();
            this.recordingLength = new System.Windows.Forms.Label();
            this.btnStop = new System.Windows.Forms.Button();
            this.btnSave = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.ddlInputDeviceName = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.ddlOutputDeviceName = new System.Windows.Forms.ComboBox();
            this.lblMinRecordingLength = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.richTextBoxCompiledAtis = new System.Windows.Forms.RichTextBox();
            this.SuspendLayout();
            // 
            // btnRecord
            // 
            this.btnRecord.Enabled = false;
            this.btnRecord.Location = new System.Drawing.Point(36, 157);
            this.btnRecord.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.btnRecord.Name = "btnRecord";
            this.btnRecord.Size = new System.Drawing.Size(110, 27);
            this.btnRecord.TabIndex = 0;
            this.btnRecord.Text = "Start Recording";
            this.btnRecord.UseVisualStyleBackColor = true;
            this.btnRecord.Click += new System.EventHandler(this.btnRecord_Click);
            // 
            // btnListen
            // 
            this.btnListen.Enabled = false;
            this.btnListen.Location = new System.Drawing.Point(36, 224);
            this.btnListen.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.btnListen.Name = "btnListen";
            this.btnListen.Size = new System.Drawing.Size(110, 27);
            this.btnListen.TabIndex = 1;
            this.btnListen.Text = "Listen";
            this.btnListen.UseVisualStyleBackColor = true;
            this.btnListen.Click += new System.EventHandler(this.btnListen_Click);
            // 
            // recordingLength
            // 
            this.recordingLength.AutoSize = true;
            this.recordingLength.Location = new System.Drawing.Point(163, 173);
            this.recordingLength.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.recordingLength.Name = "recordingLength";
            this.recordingLength.Size = new System.Drawing.Size(49, 15);
            this.recordingLength.TabIndex = 2;
            this.recordingLength.Text = "00:00:00";
            this.recordingLength.Visible = false;
            // 
            // btnStop
            // 
            this.btnStop.Enabled = false;
            this.btnStop.Location = new System.Drawing.Point(36, 191);
            this.btnStop.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.btnStop.Name = "btnStop";
            this.btnStop.Size = new System.Drawing.Size(110, 27);
            this.btnStop.TabIndex = 3;
            this.btnStop.Text = "Stop Recording";
            this.btnStop.UseVisualStyleBackColor = true;
            this.btnStop.Click += new System.EventHandler(this.btnStop_Click);
            // 
            // btnSave
            // 
            this.btnSave.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnSave.Enabled = false;
            this.btnSave.Location = new System.Drawing.Point(194, 262);
            this.btnSave.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(88, 27);
            this.btnSave.TabIndex = 4;
            this.btnSave.Text = "Save";
            this.btnSave.UseVisualStyleBackColor = true;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.Location = new System.Drawing.Point(288, 262);
            this.btnCancel.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(88, 27);
            this.btnCancel.TabIndex = 5;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // ddlInputDeviceName
            // 
            this.ddlInputDeviceName.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.ddlInputDeviceName.FormattingEnabled = true;
            this.ddlInputDeviceName.Location = new System.Drawing.Point(36, 53);
            this.ddlInputDeviceName.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.ddlInputDeviceName.Name = "ddlInputDeviceName";
            this.ddlInputDeviceName.Size = new System.Drawing.Size(339, 23);
            this.ddlInputDeviceName.TabIndex = 6;
            this.ddlInputDeviceName.SelectedIndexChanged += new System.EventHandler(this.ddlInputDeviceName_SelectedIndexChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(36, 35);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(113, 15);
            this.label1.TabIndex = 7;
            this.label1.Text = "Microphone Device:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(36, 93);
            this.label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(79, 15);
            this.label2.TabIndex = 9;
            this.label2.Text = "Listen Device:";
            // 
            // ddlOutputDeviceName
            // 
            this.ddlOutputDeviceName.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.ddlOutputDeviceName.FormattingEnabled = true;
            this.ddlOutputDeviceName.Location = new System.Drawing.Point(36, 111);
            this.ddlOutputDeviceName.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.ddlOutputDeviceName.Name = "ddlOutputDeviceName";
            this.ddlOutputDeviceName.Size = new System.Drawing.Size(339, 23);
            this.ddlOutputDeviceName.TabIndex = 8;
            this.ddlOutputDeviceName.SelectedIndexChanged += new System.EventHandler(this.ddlOutputDeviceName_SelectedIndexChanged);
            // 
            // lblMinRecordingLength
            // 
            this.lblMinRecordingLength.ForeColor = System.Drawing.Color.Red;
            this.lblMinRecordingLength.Location = new System.Drawing.Point(190, 218);
            this.lblMinRecordingLength.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblMinRecordingLength.Name = "lblMinRecordingLength";
            this.lblMinRecordingLength.Size = new System.Drawing.Size(186, 32);
            this.lblMinRecordingLength.TabIndex = 10;
            this.lblMinRecordingLength.Text = "Recording must be at least 5 seconds long.";
            this.lblMinRecordingLength.Visible = false;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(425, 35);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(84, 15);
            this.label3.TabIndex = 11;
            this.label3.Text = "Compiled ATIS";
            // 
            // richTextBoxCompiledAtis
            // 
            this.richTextBoxCompiledAtis.Enabled = false;
            this.richTextBoxCompiledAtis.Location = new System.Drawing.Point(425, 53);
            this.richTextBoxCompiledAtis.Name = "richTextBoxCompiledAtis";
            this.richTextBoxCompiledAtis.Size = new System.Drawing.Size(369, 236);
            this.richTextBoxCompiledAtis.TabIndex = 12;
            this.richTextBoxCompiledAtis.Text = "";
            // 
            // RecordAtisDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(827, 324);
            this.ControlBox = false;
            this.Controls.Add(this.richTextBoxCompiledAtis);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.lblMinRecordingLength);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.ddlOutputDeviceName);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.ddlInputDeviceName);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnSave);
            this.Controls.Add(this.btnStop);
            this.Controls.Add(this.recordingLength);
            this.Controls.Add(this.btnListen);
            this.Controls.Add(this.btnRecord);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.Name = "RecordAtisDialog";
            this.Padding = new System.Windows.Forms.Padding(10);
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Record ATIS";
            this.TopMost = true;
            this.Load += new System.EventHandler(this.RecordAtisDialog_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnRecord;
        private System.Windows.Forms.Button btnListen;
        private System.Windows.Forms.Label recordingLength;
        private System.Windows.Forms.Button btnStop;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.ComboBox ddlInputDeviceName;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox ddlOutputDeviceName;
        private System.Windows.Forms.Label lblMinRecordingLength;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.RichTextBox richTextBoxCompiledAtis;
    }
}