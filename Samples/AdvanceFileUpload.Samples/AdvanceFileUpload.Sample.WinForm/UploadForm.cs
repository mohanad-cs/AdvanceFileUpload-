﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using DevExpress.XtraBars;
using AdvanceFileUpload.Client;
using System.IO;
using AdvanceFileUpload.Application.Compression;
using AdvanceFileUpload.Application.Request;

namespace AdvanceFileUpload.Sample.WinForm
{
    public partial class UploadForm : DevExpress.XtraEditors.XtraForm
    {
        const string PauseCaption = "Pause Uploading";
        const string ResumeCaption = "Resume Uploading";
        const string StartCaption = "Start Uploading";
        const string CancelCaption = "Cancel Uploading";
        private readonly string _defaultTempDirectory = Path.GetTempPath();


        private FileUploadService _fileUploadService;
        private FileUploadBuilder _fileUploadBuilder;
        public UploadForm()
        {
            InitializeComponent();
            txtAPIBaseAdrees.Text = "http://185.227.109.88:5124";
            txtAPIKey.Text = "secret";
            spinMaxConcurrentUploads.EditValue = 4;
            spinMaxRetriesCount.EditValue = 3;
            btnPause_Resume.Enabled = false;
            btnCancel.Enabled = false;
            btnCancel.Caption = CancelCaption;
            btnPause_Resume.ImageOptions.SvgImage = Properties.Resources.CaretRightSolid8;
            btnPause_Resume.Caption = StartCaption;
            btnUpload.ItemClick += BtnUpload_ItemClick;
            btnPause_Resume.ItemClick += BtnPause_Resume_ItemClick;
            btnCancel.ItemClick += BtnCancel_ItemClick;
            btnTempDir.Click += BtnTempDir_Click;
            checkEnableCompression.EditValueChanged += CheckEnableCompression_EditValueChanged;
            this.FormClosing += UploadForm_FormClosing;
            txtFilePath.ReadOnly = true;
            txtFileSize.ReadOnly = true;
            memoEdit.AppendLine("Welcome to the Advance File Upload Sample Application.\n");
            memoEdit.AppendLine("Please select a file to upload.\n");
            memoEdit.AppendLine("You can pause, resume, or cancel the upload process.\n");
            memoEdit.ReadOnly = true;
            memoEdit.Properties.UseReadOnlyAppearance = false;
            memoEdit.Font = new Font(memoEdit.Font.FontFamily, 14, memoEdit.Font.Style);
            txtFilePath.EditValue = "Select a file to upload";
            txtFilePath.Properties.UseReadOnlyAppearance = false;
            txtFileSize.Properties.UseReadOnlyAppearance = false;
            btnTempDir.Text = _defaultTempDirectory;
            checkEnableCompression.Checked = true;
            comboBoxCompressionAlgorithm.Properties.Items.AddRange(Enum.GetValues(typeof(CompressionAlgorithmOption)).Cast<object>().ToArray());
            comboBoxCompressionAlgorithm.SelectedIndex = 0;
            comboBoxCompressionLevel.Properties.Items.AddRange(Enum.GetValues(typeof(CompressionLevelOption)).Cast<object>().ToArray());
            comboBoxCompressionLevel.SelectedIndex = 0;
        }

        private void UploadForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            if (_fileUploadService != null)
            {
                _fileUploadService.Dispose();
            }
        }

        private void ListenToUploadEvents()
        {
            if (_fileUploadService != null)
            {
                _fileUploadService.FileCompressionStarted += _fileUploadService_FileCompressionStarted;
                _fileUploadService.FileCompressionCompleted += _fileUploadService_FileCompressionCompleted;
                _fileUploadService.FileSplittingStarted += _fileUploadService_FileSplittingStarted;
                _fileUploadService.FileSplittingCompleted += _fileUploadService_FileSplittingCompleted;
                _fileUploadService.SessionCreated += _fileUploadService_SessionCreated;
                _fileUploadService.ChunkUploaded += _fileUploadService_ChunkUploaded;
                _fileUploadService.UploadProgressChanged += _fileUploadService_UploadProgressChanged;
                _fileUploadService.SessionPausing += _fileUploadService_SessionPausing;
                _fileUploadService.SessionPaused += _fileUploadService_SessionPaused;
                _fileUploadService.SessionResuming += _fileUploadService_SessionResuming;
                _fileUploadService.SessionResumed += _fileUploadService_SessionResumed;
                _fileUploadService.SessionCanceling += _fileUploadService_SessionCanceling;
                _fileUploadService.SessionCanceled += _fileUploadService_SessionCanceled;
                _fileUploadService.SessionCompleting += _fileUploadService_SessionCompleting;
                _fileUploadService.SessionCompleted += _fileUploadService_SessionCompleted;
                _fileUploadService.UploadError += _fileUploadService_UploadError;
                _fileUploadService.NetworkError += _fileUploadService_NetworkError;
                _fileUploadService.AuthenticationError += _fileUploadService_AuthenticationError;
                _fileUploadService.UploadRetryAttempt += _fileUploadService_UploadRetryAttempt;
            }


        }

        private void _fileUploadService_UploadRetryAttempt(object? sender, string e)
        {
            memoEdit.Invoke(() =>
           memoEdit.AppendLine(e + "\n"));
        }

        private void _fileUploadService_AuthenticationError(object? sender, string e)
        {
            memoEdit.Invoke(() =>
            memoEdit.AppendLine(e + "\n"));

            btnPause_Resume.Caption = StartCaption;
            btnPause_Resume.ImageOptions.SvgImage = Properties.Resources.CaretRightSolid8;
            btnPause_Resume.Enabled = true;
            btnUpload.Enabled = false;
            btnCancel.Enabled = false;
            Invoke(() =>
            {
                layoutControlGroup2.Enabled = true;
            });
            _fileUploadService?.Dispose();


        }

        private void _fileUploadService_NetworkError(object? sender, string e)
        {
            memoEdit.Invoke(() =>
            memoEdit.AppendLine(e + "\n"));
            if (_fileUploadService.CanResumeSession)
            {
                btnPause_Resume.Caption = ResumeCaption;
                btnPause_Resume.ImageOptions.SvgImage = Properties.Resources.PlaybackRateOther;
                btnCancel.Enabled = _fileUploadService.CanCancelSession;
                btnUpload.Enabled = false;
            }
        }

        private void _fileUploadService_UploadError(object? sender, string e)
        {
            memoEdit.Invoke(() =>
            memoEdit.AppendLine(e + "\n"));
            if (_fileUploadService.CanResumeSession)
            {
                btnPause_Resume.Caption = ResumeCaption;
                btnPause_Resume.ImageOptions.SvgImage = Properties.Resources.PlaybackRateOther;
                btnCancel.Enabled = _fileUploadService.CanCancelSession;
                btnUpload.Enabled = false;
            }
        }

        private void _fileUploadService_SessionCompleting(object? sender, EventArgs e)
        {
            memoEdit.Invoke(() =>
            memoEdit.AppendLine("Attempting to completing the Session...\n"));
        }

        private void _fileUploadService_SessionCompleted(object? sender, SessionCompletedEventArgs e)
        {
            Invoke(() =>
            {
                btnUpload.Enabled = true;
                btnPause_Resume.Caption = StartCaption;
                btnPause_Resume.ImageOptions.SvgImage = Properties.Resources.CaretRightSolid8;
                btnPause_Resume.Enabled = false;
                btnCancel.Enabled = false;
            });
            memoEdit.Invoke(() =>
            memoEdit.AppendLine($"Session with Id [{e.SessionId}] completed.\n"));
        }

        private void _fileUploadService_SessionCanceled(object? sender, SessionCanceledEventArgs e)
        {
            Invoke(() =>
            {
                btnUpload.Enabled = true;
                btnPause_Resume.Caption = StartCaption;
                btnPause_Resume.ImageOptions.SvgImage = Properties.Resources.CaretRightSolid8;
                btnPause_Resume.Enabled = false;
                btnCancel.Enabled = false;
            });
            memoEdit.Invoke(() =>
            memoEdit.AppendLine($"Session [{e.SessionId}] was canceled.\n"));
        }

        private void _fileUploadService_SessionCanceling(object? sender, EventArgs e)
        {
            memoEdit.Invoke(() => memoEdit.AppendLine("Attempting to canceling the Session...\n"));
        }

        private void _fileUploadService_SessionResuming(object? sender, EventArgs e)
        {
            memoEdit.Invoke(() => memoEdit.AppendLine("Attempting to resuming the Session...\n"));
        }

        private void _fileUploadService_SessionResumed(object? sender, SessionResumedEventArgs e)
        {
            memoEdit.Invoke(() =>
            memoEdit.AppendLine($"Session [{e.SessionId}] resumed.\n")); ;
        }

        private void _fileUploadService_SessionPaused(object? sender, SessionPausedEventArgs e)
        {
            memoEdit.Invoke(() =>
            memoEdit.AppendLine($"Session [{e.SessionId}] Paused.\n"));
            Invoke(() => {
                btnPause_Resume.Caption = ResumeCaption;
                btnPause_Resume.ImageOptions.SvgImage = Properties.Resources.PlaybackRateOther;
                btnCancel.Enabled = true;
            });
           
        }

        private void _fileUploadService_SessionPausing(object? sender, EventArgs e)
        {
            memoEdit.Invoke(() =>
            memoEdit.AppendLine("Attempting to Pausing the Session...\n"));
        }

        private void _fileUploadService_UploadProgressChanged(object? sender, UploadProgressChangedEventArgs e)
        {
            memoEdit.Invoke(() =>
            memoEdit.AppendLine($"Upload progress:({e.TotalUploadedChunks}/{e.TotalChunksToUpload} chunks been uploaded) RemainChunks: ({e.RemainChunks?.Count}) \n"));
            progressBarControl.Invoke(() =>
            {
                progressBarControl.Position = (int)e.ProgressPercentage;
            });
        }

        private void _fileUploadService_ChunkUploaded(object? sender, ChunkUploadedEventArgs e)
        {
            memoEdit.Invoke(() =>
               memoEdit.AppendLine($"Chunk uploaded. ChunksIndex:({e.ChunkIndex}) Chunk size: {e.ChunkSize} bytes\n")
                );

        }

        private void _fileUploadService_SessionCreated(object? sender, SessionCreatedEventArgs e)
        {
            memoEdit.Invoke(() => memoEdit.AppendLine($"Session created [{e.SessionId}] at {e.SessionStartDate}.\n"));
        }

        private void _fileUploadService_FileSplittingCompleted(object? sender, EventArgs e)
        {
            memoEdit.Invoke(() => memoEdit.AppendLine("File splitting completed.\n"));
        }

        private void _fileUploadService_FileSplittingStarted(object? sender, EventArgs e)
        {
            memoEdit.Invoke(() => memoEdit.AppendLine("File splitting started.\n"));
        }

        private void _fileUploadService_FileCompressionCompleted(object? sender, FileCompressionCompletedEventArg e)
        {


            memoEdit.Invoke(() => memoEdit.AppendLine("File compression completed.\n"));
            if (e.CompressedFileSize > 0)
            {
                memoEdit.Invoke(() => memoEdit.AppendLine($"Original file size: {GetFileSize(e.OriginalFileSize)}\n"));
                memoEdit.Invoke(() => memoEdit.AppendLine($"Compressed file size: {GetFileSize(e.CompressedFileSize)}\n"));
                long deference = e.OriginalFileSize - e.CompressedFileSize;
                memoEdit.Invoke(() => memoEdit.AppendLine($"Deference in size: {GetFileSize(deference)}\n"));
            }
            else
            {
                memoEdit.Invoke(() => memoEdit.AppendLine("No compression was applied.\n"));
            }


        }

        private void _fileUploadService_FileCompressionStarted(object? sender, EventArgs e)
        {
            memoEdit.Invoke(() => memoEdit.AppendLine("File compression started.\n"));

        }

        private void UnListenToUploadEvents()
        {
            if (_fileUploadService != null)
            {
                _fileUploadService.FileCompressionStarted -= _fileUploadService_FileCompressionStarted;
                _fileUploadService.FileCompressionCompleted -= _fileUploadService_FileCompressionCompleted;
                _fileUploadService.FileSplittingStarted -= _fileUploadService_FileSplittingStarted;
                _fileUploadService.FileSplittingCompleted -= _fileUploadService_FileSplittingCompleted;
                _fileUploadService.SessionCreated -= _fileUploadService_SessionCreated;
                _fileUploadService.ChunkUploaded -= _fileUploadService_ChunkUploaded;
                _fileUploadService.UploadProgressChanged -= _fileUploadService_UploadProgressChanged;
                _fileUploadService.SessionPausing -= _fileUploadService_SessionPausing;
                _fileUploadService.SessionPaused -= _fileUploadService_SessionPaused;
                _fileUploadService.SessionResuming -= _fileUploadService_SessionResuming;
                _fileUploadService.SessionResumed -= _fileUploadService_SessionResumed;
                _fileUploadService.SessionCanceling -= _fileUploadService_SessionCanceling;
                _fileUploadService.SessionCanceled -= _fileUploadService_SessionCanceled;
                _fileUploadService.SessionCompleting -= _fileUploadService_SessionCompleting;
                _fileUploadService.SessionCompleted -= _fileUploadService_SessionCompleted;
                _fileUploadService.UploadError -= _fileUploadService_UploadError;
                _fileUploadService.NetworkError -= _fileUploadService_NetworkError;
                _fileUploadService.AuthenticationError -= _fileUploadService_AuthenticationError;
                _fileUploadService.UploadRetryAttempt -= _fileUploadService_UploadRetryAttempt;

            }
        }
        private void CheckEnableCompression_EditValueChanged(object? sender, EventArgs e)
        {
            if (!checkEnableCompression.Checked)
            {
                comboBoxCompressionAlgorithm.Enabled = false;
                comboBoxCompressionLevel.Enabled = false;
            }
            else
            {
                comboBoxCompressionAlgorithm.Enabled = true;
                comboBoxCompressionLevel.Enabled = true;
            }
        }



        private void BtnTempDir_Click(object? sender, EventArgs e)
        {
            using (FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog())
            {
                if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
                {
                    btnTempDir.Text = folderBrowserDialog.SelectedPath;
                }
            }
        }

        private async void BtnCancel_ItemClick(object sender, ItemClickEventArgs e)
        {
            try
            {
                if (_fileUploadService.CanCancelSession)
                {
                    await _fileUploadService.CancelUploadAsync();
                    btnPause_Resume.Caption = StartCaption;
                    btnPause_Resume.ImageOptions.SvgImage = Properties.Resources.CaretRightSolid8;
                    btnCancel.Enabled = false;
                    btnUpload.Enabled = true;
                    btnPause_Resume.Enabled = false;

                }
            }
            catch (UploadException)
            {
            }
        }

        private void ClearProgress()
        {
            progressBarControl.Position = 0;
            memoEdit.Text = string.Empty;
        }
        private async void BtnPause_Resume_ItemClick(object sender, ItemClickEventArgs e)
        {

            try
            {
                switch (e.Item.Caption)
                {
                    case StartCaption:
                        btnUpload.Enabled = false;
                        ClearProgress();
                        e.Item.Caption = PauseCaption;
                        e.Item.ImageOptions.SvgImage = Properties.Resources.PauseBold;
                        btnCancel.Enabled = true;

                        layoutControlGroup2.Enabled = false;
                        if (_fileUploadService != null)
                        {
                            UnListenToUploadEvents();
                            _fileUploadService.Dispose();
                            _fileUploadService = null;
                        }
                        if (string.IsNullOrWhiteSpace(txtAPIBaseAdrees.Text))
                        {

                            txtAPIBaseAdrees.ErrorText = "The Api Base Address is required";
                            return;
                        }
                        txtAPIBaseAdrees.ErrorText = string.Empty;
                        if (string.IsNullOrWhiteSpace(txtAPIKey.Text))
                        {

                            txtAPIKey.ErrorText = "The Api Key is required";
                            return;
                        }
                        txtAPIKey.ErrorText = string.Empty;
                        _fileUploadBuilder = FileUploadBuilder.New(txtAPIBaseAdrees.Text.Trim())
                            .WithAPIKey(txtAPIKey.Text)
                            .WithCompressionOption(() =>
                            {
                                if (checkEnableCompression.Checked)
                                {
                                    return new CompressionOption()
                                    {
                                        Algorithm = (CompressionAlgorithmOption)comboBoxCompressionAlgorithm.EditValue,
                                        Level = (CompressionLevelOption)comboBoxCompressionLevel.EditValue
                                    };
                                }
                                return null;
                            })
                            .WithTempDirectory(btnTempDir.Text)
                            .WithMaxRetriesCount((int)spinMaxRetriesCount.Value)
                            .WithMaxConcurrentUploads((int)spinMaxConcurrentUploads.Value)
                            .WithRequestTimeOut(requestTimeOutEdit.TimeSpan);

                        _fileUploadService = _fileUploadBuilder.Build();

                        ListenToUploadEvents();
                        await _fileUploadService.UploadFileAsync(txtFilePath.Text);
                        break;
                    case PauseCaption:
                        if (_fileUploadService.CanPauseSession)
                        {
                            e.Item.Caption = ResumeCaption;
                            e.Item.ImageOptions.SvgImage = Properties.Resources.PlaybackRateOther;
                            btnCancel.Enabled = true;
                            await _fileUploadService.PauseUploadAsync();
                        }
                        else
                        {
                            if (_fileUploadService.IsSessionCompleted || _fileUploadService.IsSessionCanceled)
                            {

                            }
                        }
                        break;
                    case ResumeCaption:
                        if (_fileUploadService.CanResumeSession)
                        {
                            e.Item.Caption = PauseCaption;
                            e.Item.ImageOptions.SvgImage = Properties.Resources.PauseBold;
                            btnCancel.Enabled = true;
                            await _fileUploadService.ResumeUploadAsync();
                        }
                        else
                        {
                            if (_fileUploadService.IsSessionCompleted || _fileUploadService.IsSessionCanceled)
                            {

                            }
                        }

                        break;
                    default:
                        break;
                }
                Invoke(() =>
                {
                    memoEdit.Focus();
                });
            }
            catch (OperationCanceledException)
            {
            }
            catch (UploadException)
            {

            }
        }

        private void BtnUpload_ItemClick(object sender, ItemClickEventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                //openFileDialog.Filter = "PDF files (*.pdf)|*.pdf"; // Corrected filter syntax for PDF files  
                openFileDialog.Multiselect = false;
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    string filePath = openFileDialog.FileName;
                    txtFilePath.Text = filePath;
                    txtFileSize.Text = GetFileSize(new System.IO.FileInfo(filePath).Length);
                    btnPause_Resume.Caption = StartCaption;
                    btnPause_Resume.ImageOptions.SvgImage = Properties.Resources.CaretRightSolid8;
                    btnPause_Resume.Enabled = true;
                }
                layoutControlGroup2.Enabled = true;
            }
        }

        private static string GetFileSize(long Length)
        {
            if (Length < 0)
            {
                throw new ArgumentException("File size cannot be negative.");
            }

            if (Length == 0)
            {
                return "0 B";
            }

            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            int i = (int)Math.Floor(Math.Log(Length, 1024));
            double size = Length / Math.Pow(1024, i);

            return $"{size:0.##} {sizes[i]}";
        }

    }


}