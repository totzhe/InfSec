﻿using System.Collections;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Threading;
using GalaSoft.MvvmLight;
using System;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Threading;
using Steganography.Encoders;
using Steganography.Signing;

namespace Steganography.ViewModel
{
    /// <summary>
    /// This class contains properties that the main View can data bind to.
    /// <para>
    /// See http://www.galasoft.ch/mvvm
    /// </para>
    /// </summary>
    public class SignatureViewModel : ViewModelBase
    {
        private string _unsignedImagePath;
        private string _signedImagePath;
        private string _status;
        private EncoderViewModel _selectedEncoder;


        public string UnsignedImagePath
        {
            get
            {
                return _unsignedImagePath;
            }
            set
            {
                if (_unsignedImagePath == value) return;
                _unsignedImagePath = value;
                RaisePropertyChanged(() => UnsignedImagePath);
            }
        }


        public string SignedImagePath
        {
            get
            {
                return _signedImagePath;
            }
            set
            {
                if (_signedImagePath == value) return;
                _signedImagePath = value;
                ResetStatus();
                RaisePropertyChanged(() => SignedImagePath);
            }
        }


        public string Status
        {
            get
            {
                return _status;
            }
            set
            {
                if (_status == value) return;
                _status = value;
                RaisePropertyChanged(() => Status);
            }
        }

        public ObservableCollection<EncoderViewModel> Encoders { get; private set; }

        public EncoderViewModel SelectedEncoder
        {
            get { return _selectedEncoder; }
            set
            {
                _selectedEncoder = value;
                RaisePropertyChanged(() => SelectedEncoder);
            }
        }

        public RelayCommand OpenUnsignedImgCommand { get; private set; }
        public RelayCommand OpenSignedImgCommand { get; private set; }
        public RelayCommand SignCommand { get; private set; }
        public RelayCommand CheckCommand { get; private set; }


        /// <summary>
        /// Initializes a new instance of the MainViewModel class.
        /// </summary>
        public SignatureViewModel()
        {
            _status = "";
            _unsignedImagePath = @"Resources\Document.png";
            OpenUnsignedImgCommand = new RelayCommand(OpenUnsignedImage);
            OpenSignedImgCommand = new RelayCommand(OpenSignedImage);
            SignCommand = new RelayCommand(Sign, () => !string.IsNullOrEmpty(UnsignedImagePath));
            CheckCommand = new RelayCommand(CheckSignature, () => !string.IsNullOrEmpty(SignedImagePath));
        }

        private void OpenUnsignedImage()
        {
            var path = ShowOpenFileDialog();
            if (path != null)
            {
                UnsignedImagePath = path;
            }
        }

        private void OpenSignedImage()
        {
            var path = ShowOpenFileDialog();
            if (path != null)
            {
                SignedImagePath = path;
            }
        }

        private string ShowOpenFileDialog()
        {
            var dlg = new Microsoft.Win32.OpenFileDialog { DefaultExt = ".bmp", Filter = "Images|*.bmp;*.png" };
            var result = dlg.ShowDialog();

            return result == true ? dlg.FileName : null;
        }

        private void Sign()
        {
            try
            {
                var signer = new SimpleHashSigner(new LsbEncoder());
                using (var image = (Bitmap)Image.FromFile(UnsignedImagePath, true))
                {
                    var newImg = signer.Sign(image);
                    var newPath = Path.Combine(Path.GetDirectoryName(UnsignedImagePath),
                        string.Format("{0}_{1}_{2}{3}", Path.GetFileNameWithoutExtension(UnsignedImagePath), "SIGNED",
                            GetTimeStamp(), Path.GetExtension(UnsignedImagePath))
                        );
                    newImg.Save(newPath);
                    SignedImagePath = newPath;
                }
            }
            catch (Exception e)
            {
                System.Windows.MessageBox.Show(e.Message);
            }
        }

        private void CheckSignature()
        {
            try
            {
                ResetStatus();
                var signer = new SimpleHashSigner(new LsbEncoder());
                using (var image = (Bitmap)Image.FromFile(SignedImagePath, true))
                {
                    var result = signer.ValidateSignature(image);
                    SetStatus(result.SignatureIsValid);
                    // сохраняем изображение с пометками проверки
                    var path = Path.Combine(
                        Path.GetDirectoryName(UnsignedImagePath),
                        string.Format("{0}_{1}_{2}{3}", Path.GetFileNameWithoutExtension(UnsignedImagePath),
                            "INVALID_SIGNATURE",
                            GetTimeStamp(),
                            Path.GetExtension(UnsignedImagePath))
                        );
                    result.ImageWithValidationMarks.Save(path);
                    Process.Start(path);
                }
            }
            catch (Exception e)
            {
                SetStatus(false);
                System.Windows.MessageBox.Show(e.Message);
            }
        }

        private void ResetStatus()
        {
            Status = "";
        }

        private void SetStatus(bool signatureIsValid)
        {
            if (signatureIsValid)
            {
                Status = "Signature is valid.";
            }
            else
            {
                Status = "Signature is not valid.";
            }
        }

        private string GetTimeStamp()
        {
            return DateTime.Now.ToString("yyyyMMddHHmmss");
        }


        //public override void Cleanup()
        //{
        //    // Clean up if needed

        //    base.Cleanup();
        //}
    }
}