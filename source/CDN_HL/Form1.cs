using CDN_HL.DN_HLDataSetTableAdapters;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace CDN_HL
{
    public partial class Form1 : Form
    {
        public string _ImgFolderPath = string.Empty;
        public string _ImgFolderDonePath = string.Empty;
        public string _ImgFolderArchivePath = string.Empty;
        public string _logFile = string.Empty;
        public string _errorFile = string.Empty;
        static int _ixSelectLength = 0;             //mousedown event (hold index selected length) on txtImgFilename Click
        static int _iClickCount = 0;                //keep track of number clicks on txtImgFilename

        const string _strVietAlpha_A = "aàảãáạăằẳẵắặâầẩẫấậ";         //[0]
        const string _strVietAlpha_D = "dđ";                         //[1]
        const string _strVietAlpha_E = "eèẻẽéẹêềểễếệ";               //[2]
        const string _strVietAlpha_I = "iìỉĩíị";                     //[3]
        const string _strVietAlpha_O = "oòỏõóọôồổỗốộơờởỡớợ";         //[4]
        const string _strVietAlpha_U = "uùủũúụưừửữứự";               //[5]
        const string _strVietAlpha_Y = "yỳỷỹýỵ";                     //[6]
        enum Days { Sun, Mon, Tue, Wed, Thu, Fri, Sat };

        public Form1()
        {
            ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            //// 1. Can't Debug - Error:
            //// Module is optimized and the debugger option 'Just My Code' is enabled.
            //// How to fix this issue:
            //// https://docs.microsoft.com/en-us/visualstudio/debugger/just-my-code?view=vs-2019
            //// To enable or disable Just My Code in Visual Studio, 
            ////    under Tools > Options (or Debug > Options) > Debugging > General, select or deselect Enable Just My Code.
            ////
            //// 2. Can't open Ctrl + Shift + F (Find in File)
            //// How to fix this isue:
            //// a. Go To Window
            //// b. Reset Window Layout
            ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            try
            {
                //this.Font = new Font("Microsoft Sans Serif", 12);  NGUYỄN HOÀNG PHƯỢNG

                InitializeComponent();
            }
            catch (Exception e)
            {
                string strErr = e.Message;
            }
        }

        #region Run ONCE
        // RUN THIS FUNCTION ONLY ONCE TO: change the DB from the old format [Ho,Dem,Ten] to the new format [HoTen] AND extract filenumber from filename
        // trying to parse Filenumber from the filename and Convert_VN_To_Eng for Fullname
        // 1. Open/Copy from RemovableDisk:		D:\CDN\LinhTu\DB\linhtu.mdb
        // 2. Select table: tblHuongLinh and Export data to Excel file 	C:\CDN\CDN_HL\CDN_HL\DB\tblHL.xlsx
        // 3. Open tblHL.xlsx file and ADD these columns: HoTen, Fullname, FileNumber, InsertDate, UpdateDate, Note
        // 4. In tblHL.xlsx file, combines [Ho,Dem,Ten] to [HoTen] uses this format: [=D2&" "&E2&" "&F2]
        // 5. Delete [HL_ID, GC_ID, LienHeVoi_GC, Ho, Dem, Ten, Tho, GioiTinh, SinhTai, MatGio, MatTai, NhapLiem, ChonThieu, int_ViTriHinh] columns in tblHL.xlsx file
        // 6. Rename [int_ViTriCot] to [ViTriCot]
        // 7. Must rename the excel tab [tblHuongLinh] to [tblHL] and save the file
        // 8. Open Access >> File >> New DB(Empty) >> import Data from excel file >> tblHL.xlsx and the TABLE as tblHL, and then SAVE the DB AS >> DN_HL.accdb
        // 9. In this application MUST: add these columns to the gridview: Fullname,FileNumber,InsertDate,UpdateDate
        // 10. And RUN THIS APPLICATION ONLY ONCE!!
        // 11. DO NOT USE THIS APPLICATION EVER AGAIN.
        private void ParseFilenumberAndConvert_VN_TO_ENG()
        {
            if (datasGridView.Rows.Count > 1)
            {

                //foreach (DataGridViewRow dr in dataGridView.Rows)
                foreach (DataRow dr in dN_HLDataSet.tblHL.Rows)
                {
                    string strTempFilenumber = dr["HinhFileNamePath"].ToString();
                    string[] astrFilenumber = strTempFilenumber.Split(' ');
                    if (astrFilenumber.Count() > 1)
                        dr["FileNumber"] = astrFilenumber[0];

                    string strEnglishHoTen = Convert_VN_To_Eng(dr["HoTen"].ToString().Trim());
                    string strEnglishPhapDanh = Convert_VN_To_Eng(dr["PhapDanh"].ToString().Trim());

                    ////set Fullname + PD if available by remove Vietnamese accent for searching purpose only
                    //if (strEnglishHoTen.Length > 0 && strEnglishPhapDanh.Length > 0)
                    //    dr["Fullname"] = strEnglishHoTen + " PD " + strEnglishPhapDanh;
                    //else 
                    if (strEnglishHoTen.Length > 0)
                        dr["Fullname"] = strEnglishHoTen;

                    if (strEnglishPhapDanh.Length > 0)
                        dr["FullPhapDanh"] = strEnglishPhapDanh;

                    //dr.Cells["Fullname"].Value = strEnglishHoTen;

                    //dr["InsertDate"] = DateTime.Now.ToString("G");  // In order for these columns to work in this section
                    //dr["UpdateDate"] = DateTime.Now.ToString("G");  // Must add these columns to the DataGridViewRow, for NOW, remove them
                }

                tblHLBindingSource.EndEdit();
                tblHLTableAdapter.Update(this.dN_HLDataSet.tblHL);  //Update the HL_DB
            }

        }

        #endregion Run ONCE

        private void Form1_Load(object sender, EventArgs e)
        {
            if (!VerifyAndSetupDataSourceAndWorkingFolders())
            {
                // failed to verify or setup all settings
                MessageBox.Show("Unable to set up Huong Linh access database. Shutting down CDN_HL application!",
                    @"Invalid CDN_HL Database Setting");
                Environment.Exit(1);
            }

            //Add References: Assemblies >> System.Configuration
            _ImgFolderPath = ConfigurationManager.AppSettings.Get("ImgFolderPath");
            _ImgFolderDonePath = ConfigurationManager.AppSettings.Get("ImgFolderDonePath");
            _ImgFolderArchivePath = ConfigurationManager.AppSettings.Get("ImgFolderArchivePath");
            _logFile = $"{ConfigurationManager.AppSettings.Get("ErrLogPath")}Log.txt";
            _errorFile = $"{ConfigurationManager.AppSettings.Get("ErrLogPath")}{ConfigurationManager.AppSettings.Get("ErrLogFile")}";

            Location = new Point(20, 20);   //Starts the Form at this location
            tabSearch.Select(); //Active the tab control and select the tabSearch
            RefreshSearchTab();

            //--------------------------------------------------------------------------------------
            //- 
            //- ////////Run this function only ONCE when change the DB format from the old to the new
            //- //////ParseFilenumberAndConvert_VN_TO_ENG();
            //- 
            //--------------------------------------------------------------------------------------

        }
        private void datasGridViewBinding(object objDataSource)
        {
            datasGridView.DataSource = null;
            //datasGridView.Rows.Clear();
            //datasGridView.Columns.Clear();
            datasGridView.DataSource = objDataSource;
            //datasGridView.Rows[0].Selected = true;
            datasGridView.Focus();
        }

        /// <summary>
        /// 1. ClearAllSearchFields() - 2. Fill the dN_HLDataSet.tblHL - 3. Bind the datasGridView - then Populate the HL fields: Ten, ViTriHinh, ViTriCot, etc...
        /// </summary>
        private void RefreshSearchTab()
        {
            ClearAllSearchFields();
            ResetBackGroundColorForAllFieldsOnSearchTab();
            dN_HLDataSet.tblHL.DefaultView.RowFilter = "";
            dN_HLDataSet.tblHL.DefaultView.Sort = "";

            dN_HLDataSet.tblHL.AcceptChanges();
            try
            {
                tblHLTableAdapter.Fill(dN_HLDataSet.tblHL);
            }
            catch (InvalidOperationException)
            {
                MessageBox.Show($"Unable to access the CDN_HL database! Install the Office.AccessDB.Redistribution.32bit.exe or" +
                    $" Rebuild the solution in x86 platform!", "Exit Application");
                Environment.Exit(1);
            }
            catch
            {
                // Auto restart the application to enforce the updated application settings
                Application.Restart();
            }

            tblHLBindingSource.DataSource = dN_HLDataSet.tblHL;
            tblHLBindingSource.Position = 0;
            datasGridViewBinding(tblHLBindingSource);
            DisplayImage();
        }
        private void btnsRefresh_Click(object sender, EventArgs e)
        {
            RefreshSearchTab();
        }

        #region - Search Tab Section
        /// <summary>
        /// Re-Drawing picsBoxHL with the filename in the lblsOrigFilename.Text; and then ResetBackGroundColorForAllFieldsOnSearchTab()
        /// </summary>
        private void DisplayImage()
        {
            string strImgFileNamePath = "";

            if (lblsOrigFilename.Text.Trim().Length > 0 && txtsFilename.Text.Trim().Length > 0)
            {
                //use lblsOrigFilename.Text for Image Filename
                strImgFileNamePath = _ImgFolderDonePath + lblsOrigFilename.Text.Trim();
            }

            if (strImgFileNamePath != "")
            {
                if (File.Exists(strImgFileNamePath))
                {
                    Bitmap bitImageFileOrig = new Bitmap(strImgFileNamePath);
                    Bitmap bitImageFileCopy = new Bitmap((Image)bitImageFileOrig);

                    picsBoxHL.SizeMode = PictureBoxSizeMode.StretchImage;    //in order to have any image "resize" to fit a picturebox, you must set this  
                    picsBoxHL.Width = 170; // 440;   // 300;   // 580;
                    picsBoxHL.Height = 200; //450;  // 260;   // 500;
                    picsBoxHL.Image = (Image)bitImageFileCopy;
                    piciBoxHL.Refresh();
                    picsBoxHL.Tag = txtsHoTen.Text;

                    bitImageFileOrig.Dispose(); //release the Original image file to allow this file to be deleted in this program
                    //bitImageFileCopy.Dispose();   //DO NOT >> SET bitImageFileCopy.Dispose(); << IT WILL CAUSE THE APPLICATION STOP RUNNING!!

                    ResetBackGroundColorForAllFieldsOnSearchTab();
                }
                else
                {
                    picsBoxHL.Image = null;  //Hinh Not found.
                    lblsErrorMsg.Text = "Hinh Not Found.";
                }
            }
            else
            {
                picsBoxHL.Image = null;
                lblsErrorMsg.Text = "Hinh Not Found.";
            }

        }
        /// <summary>
        /// Remove the Vietnamese accent from the Name and Phap Danh.
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static String Convert_VN_To_Eng(String str)
        {
            str = str.Replace("ầ", "a").Replace("ấ", "a").Replace("ậ", "a").Replace("ẩ", "a").Replace("ẫ", "a").Replace("ằ", "a").Replace("ắ", "a").Replace("ặ", "a").Replace("ẳ", "a").Replace("ẵ", "a").Replace("â", "a").Replace("ă", "a").Replace("ả", "a").Replace("ã", "a").Replace("ạ", "a").Replace("á", "a").Replace("à", "a");
            str = str.Replace("ề", "e").Replace("ế", "e").Replace("ệ", "e").Replace("ể", "e").Replace("ễ", "e").Replace("ẽ", "e").Replace("ê", "e").Replace("ẹ", "e").Replace("ẻ", "e").Replace("è", "e").Replace("é", "e");
            str = str.Replace("ì", "i").Replace("í", "i").Replace("ị", "i").Replace("ỉ", "i").Replace("ĩ", "i");
            str = str.Replace("ồ", "o").Replace("ố", "o").Replace("ộ", "o").Replace("ổ", "o").Replace("ỗ", "o").Replace("ơ", "o").Replace("ờ", "o").Replace("ớ", "o").Replace("ợ", "o").Replace("ở", "o").Replace("ỡ", "o").Replace("ô", "o").Replace("ọ", "o").Replace("ỏ", "o").Replace("õ", "o").Replace("ò", "o").Replace("ó", "o");
            str = str.Replace("ừ", "u").Replace("ứ", "u").Replace("ự", "u").Replace("ử", "u").Replace("ữ", "u").Replace("ư", "u").Replace("ụ", "u").Replace("ủ", "u").Replace("ũ", "u").Replace("ù", "u").Replace("ú", "u");
            str = str.Replace("ỳ", "y").Replace("ý", "y").Replace("ỵ", "y").Replace("ỷ", "y").Replace("ỹ", "y");
            str = str.Replace("đ", "d");

            str = str.Replace("Ầ", "A").Replace("Ấ", "A").Replace("Ậ", "A").Replace("Ẩ", "A").Replace("Ẫ", "A").Replace("Ằ", "A").Replace("Ắ", "A").Replace("Ặ", "A").Replace("Ẳ", "A").Replace("Ẵ", "A").Replace("Â", "A").Replace("Ă", "A").Replace("Ả", "A").Replace("Ã", "A").Replace("Ạ", "A").Replace("Á", "A").Replace("À", "A");
            str = str.Replace("Ề", "E").Replace("Ế", "E").Replace("Ệ", "E").Replace("Ể", "E").Replace("Ễ", "E").Replace("Ẽ", "E").Replace("Ê", "E").Replace("Ẹ", "E").Replace("Ẻ", "E").Replace("È", "E").Replace("É", "E");
            str = str.Replace("Ì", "I").Replace("Í", "I").Replace("Ị", "I").Replace("Ỉ", "I").Replace("Ĩ", "I");
            str = str.Replace("Ồ", "O").Replace("Ố", "O").Replace("Ộ", "O").Replace("Ổ", "O").Replace("Ỗ", "O").Replace("Ơ", "O").Replace("Ờ", "O").Replace("Ớ", "O").Replace("Ợ", "O").Replace("Ở", "O").Replace("Ỡ", "O").Replace("Ô", "O").Replace("Ọ", "O").Replace("Ỏ", "O").Replace("Õ", "O").Replace("Ò", "O").Replace("Ó", "O");
            str = str.Replace("Ừ", "U").Replace("Ứ", "U").Replace("Ự", "U").Replace("Ử", "U").Replace("Ữ", "U").Replace("Ư", "U").Replace("Ụ", "U").Replace("Ủ", "U").Replace("Ũ", "U").Replace("Ù", "U").Replace("Ú", "U");
            str = str.Replace("Ỳ", "Y").Replace("Ý", "Y").Replace("Ỵ", "Y").Replace("Ỷ", "Y").Replace("Ỹ", "Y");
            str = str.Replace("Đ", "D");

            //Clean all the left over again
            str = str.Replace("Á", "A").Replace("Ả", "A").Replace("À", "A");
            str = str.Replace("È", "E").Replace("É", "E").Replace("Ẻ", "E");
            str = str.Replace("Í", "I").Replace("Ì", "I").Replace("Ỉ", "I").Replace("Ị", "I");
            str = str.Replace("Ò", "O").Replace("Ó", "O").Replace("Ỏ", "O");
            str = str.Replace("Ù", "U").Replace("Ú", "U").Replace("Ủ", "U");
            str = str.Replace("Ỳ", "Y").Replace("Ý", "Y").Replace("Ỷ", "Y");

            str = str.Replace("á", "a").Replace("ả", "a").Replace("à", "a");
            str = str.Replace("è", "e").Replace("ẻ", "e").Replace("é", "e");
            str = str.Replace("í", "i").Replace("ị", "i");
            str = str.Replace("ỏ", "o").Replace("ò", "o").Replace("ó", "o");
            str = str.Replace("ú", "u").Replace("ủ", "u").Replace("ù", "u");
            str = str.Replace("ỷ", "y");

            //remove special char
            str = str.Replace("D", "D");
            str = str.Replace("̣", "");
            str = str.Replace("̃", "");

            return str;
        }
        private void btnsSave_Click(object sender, EventArgs e)
        {
            SaveEdit();
        }
        /// <summary>
        /// Delete the HL record if user SELECT the record in the datasGridView and PRESSES on the "Delete" key button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void datasGridView_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                if (datasGridView.SelectedRows.Count > 0)
                {
                    DialogResult result = MessageBox.Show("Do you want to delete this HL", "Message", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

                    if (result == DialogResult.Yes)
                    {
                        int iGridRowIndex = datasGridView.SelectedRows[0].Index;
                        int iRowID = int.Parse(datasGridView[0, iGridRowIndex].Value.ToString());

                        datasGridView.Rows.RemoveAt(datasGridView.SelectedRows[0].Index);

                        string sqlExpression = "ID = " + iRowID.ToString();

                        DataRow[] arrDataRow = dN_HLDataSet.tblHL.Select(sqlExpression);   //.Select("id = 2782");

                        if (arrDataRow.Length > 0)
                            arrDataRow[0].Delete();                     //Remove this record from the dN_HLDataSet.tblHL

                        tblHLBindingSource.EndEdit();

                        tblHLTableAdapter.Update(dN_HLDataSet.tblHL);   //update the DB
                        dN_HLDataSet.tblHL.AcceptChanges();             //update the DataSet

                        tblHLTableAdapter.Fill(dN_HLDataSet.tblHL);
                        datasGridView.Focus();

                        DisplayImage();                                 //display the current selected item in datasGridView

                        txtsSearch.Text = "Successfully deleted!";
                        txtsSearch.ForeColor = System.Drawing.Color.Red;          //Text 
                        txtsSearch.BackColor = System.Drawing.Color.Yellow;       //Background
                    }
                    else
                    {
                        txtsSearch.Text = "Not deleted!";
                    }
                }
                else
                {
                    MessageBox.Show("Please select the entire row '>' to delete HL.", "Message", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            else
            {
                DisplayImage();
            }
            //datasGridView.ClearSelection();
        }
        private void datasGridView_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            DisplayImage();
        }
        private void datasGridView_KeyPress(object sender, KeyPressEventArgs e)
        {
            DisplayImage();
        }
        private void datasGridView_KeyUp(object sender, KeyEventArgs e)
        {
            DisplayImage();
        }
        private void datasGridView_CellMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            DisplayImage();
        }
        /// <summary>
        /// Set lblsErrorMsg.Text = "";	txtsSearch.Text = ""; txtsFNumbSearch.Text = ""; cksPhapDanh.Checked = false; txtsFilename.Text = "";
        /// </summary>
        private void ClearAllSearchFields()
        {
            lblsErrorMsg.Text = "";
            txtsSearch.Text = "";
            txtsSearch.ForeColor = System.Drawing.Color.Black;       //Text 
            txtsSearch.BackColor = System.Drawing.Color.White;       //Background
            txtsFNumbSearch.Text = "";
            cksPhapDanh.Checked = false;
        }
        private void ResetBackGroundColorForAllFieldsOnSearchTab()
        {
            lblsErrorMsg.Text = "";
            txtsViTriHinh.ForeColor = System.Drawing.Color.Red;          //Text 
            txtsViTriHinh.BackColor = System.Drawing.Color.Yellow;       //Background

            txtsViTriCot.ForeColor = System.Drawing.Color.Blue;
            txtsViTriCot.BackColor = System.Drawing.Color.Yellow;

            txtsHoTen.ForeColor = System.Drawing.Color.Black;
            txtsHoTen.BackColor = System.Drawing.Color.White;

            txtsPhapDanh.ForeColor = System.Drawing.Color.Black;
            txtsPhapDanh.BackColor = System.Drawing.Color.White;

            txtsSinh.ForeColor = System.Drawing.Color.Black;
            txtsSinh.BackColor = System.Drawing.Color.White;

            txtsTu.ForeColor = System.Drawing.Color.Black;
            txtsTu.BackColor = System.Drawing.Color.White;

            txtsTuAL.ForeColor = System.Drawing.Color.Black;
            txtsTuAL.BackColor = System.Drawing.Color.White;

            txtsFileNumber.ForeColor = System.Drawing.Color.Black;
            txtsFileNumber.BackColor = System.Drawing.Color.White;

            txtsFilename.ForeColor = System.Drawing.Color.Black;
            txtsFilename.BackColor = System.Drawing.Color.White;

            txtsNote.ForeColor = System.Drawing.Color.Black;
            txtsNote.BackColor = System.Drawing.Color.White;
        }
        private void ChangeSearchTabFieldsBackGroundColor()
        {
            if (lblsOrigViTriHinh.Text.Trim() != txtsViTriHinh.Text.Trim())
                txtsViTriHinh.BackColor = System.Drawing.Color.Gainsboro;        //Background

            if (lblsOrigViTriCot.Text.Trim() != txtsViTriCot.Text.Trim())
                txtsViTriCot.BackColor = System.Drawing.Color.Gainsboro;

            if (lblsOrigHoTen.Text.Trim() != txtsHoTen.Text.Trim())
                txtsHoTen.BackColor = System.Drawing.Color.Gainsboro;

            if (lblsOrigPhapDanh.Text.Trim() != txtsPhapDanh.Text.Trim())
                txtsPhapDanh.BackColor = System.Drawing.Color.Gainsboro;

            if (lblsOrigSinh.Text.Trim() != txtsSinh.Text.Trim())
                txtsSinh.BackColor = System.Drawing.Color.Gainsboro;

            if (lblsOrigTu.Text.Trim() != txtsTu.Text.Trim())
                txtsTu.BackColor = System.Drawing.Color.Gainsboro;

            if (lblsOrigTuAl.Text.Trim() != txtsTuAL.Text.Trim())
                txtsTuAL.BackColor = System.Drawing.Color.Gainsboro;

            if (lblsOrigFilename.Text.Trim() != txtsFilename.Text.Trim())
                txtsFilename.BackColor = System.Drawing.Color.Gainsboro;

            if (lblsOrigFileNumber.Text.Trim() != txtsFileNumber.Text.Trim())
                txtsFileNumber.BackColor = System.Drawing.Color.Gainsboro;
        }
        /// <summary>
        /// Call the "Search()" when user hits the "Enter" key in the txtsSearch
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
		private void txtsSearch_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)13)   //Enter key press
            {
                Search();
            }
        }
        /// <summary>
        /// Clear All the Search Fields when user hits the "Delete" or "Home" key in the txtsSearch
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void txtsSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Home || e.KeyCode == Keys.Delete) //(e.KeyCode == Keys.Back)
            {
                ClearAllSearchFields();
            }
        }
        private void txtsSearch_MouseClick(object sender, MouseEventArgs e)
        {
            ClearAllSearchFields();
        }
        private void btnsSearch_Click(object sender, EventArgs e)
        {
            Search();
        }
        private void ReleaseSearchImagePictureBox()
        {
            //release the Picturebox's image in order for the File.Move() to work!
            if (picsBoxHL.Image != null)
            {
                picsBoxHL.Image.Dispose();
                picsBoxHL.Image = null;
                //picsBoxHL.Invalidate();
                picsBoxHL.Refresh();
            }
        }
        private void SaveEdit()
        {
            try
            {   //1 Ngô Dung Pd Nhật Đoan 1919-2002.jpg
                lblsErrorMsg.Text = "";

                string strEnglishHoTen = Convert_VN_To_Eng(txtsHoTen.Text.Trim());
                string strEnglishPhapDanh = Convert_VN_To_Eng(txtsPhapDanh.Text.Trim());

                if (strEnglishHoTen.Length > 0)
                    lblsFullname.Text = strEnglishHoTen;            //tblHLBindingSource - Fullname >> the Update command will update this field's value with the dataSet

                if (strEnglishPhapDanh.Length > 0)
                    lblsFullPhapDanh.Text = strEnglishPhapDanh;     //tblHLBindingSource - FullPhapDanh >> 

                txtsDtUpdate.Text = DateTime.Now.ToString("G");

                //Replace the txtsFilename.Text with the new txtsFileNumber.Text
                if (txtsFileNumber.Text.Trim() != lblsOrigFileNumber.Text)
                    txtsFilename.Text = txtsFilename.Text.Replace(lblsOrigFileNumber.Text, txtsFileNumber.Text.Trim());

                ////Don't need to change the filename if HoTen, PhapDanh, Sinh, Tu, or TuAL changed!!
                //string strNewFilename = ReFormatImageFileName();
                //RENAME the imagefilename in imgFolderDonePath before save image filename in HL_DB

                if (lblsOrigFilename.Text != txtsFilename.Text.Trim())
                {
                    ReleaseSearchImagePictureBox();
                    Util.FileSaveAs(_ImgFolderDonePath, _ImgFolderDonePath, lblsOrigFilename.Text, txtsFilename.Text.Trim());
                    DisplayImage();                                 //refresh the image with the new filename
                }

                ChangeSearchTabFieldsBackGroundColor();

                tblHLBindingSource.EndEdit();                       //the lblsOrigHoTen.Text.Trim() IS ALWAYS == txtsHoTen.Text.Trim()
                dN_HLDataSet.tblHL.AcceptChanges();                 //Update the DataSet
                tblHLTableAdapter.Update(dN_HLDataSet.tblHL);       //Update the HL_DB
                tblHLBindingSource.ResetBindings(true);
                datasGridView.DataSource = tblHLBindingSource;      //Refresh datasGridView with the new Filename if changed
                datasGridView.Refresh();
                datasGridView.Focus();

                lblsErrorMsg.Text = "Saved!! !!!!";
                Util.LogAMessage(_logFile, $"Updated '{_ImgFolderDonePath}{lblsOrigFilename.Text}'");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Message", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Util.LogAMessage(_errorFile, $"Failed to update '{_ImgFolderDonePath}{lblsOrigFilename.Text}'. Exception: '{ex.Message}'");
                tblHLBindingSource.ResetBindings(false);
            }
        }
        private bool SearchFullnameAsIs(string strSearch)
        {
            bool bDataFound = false;
            string strRowFilter = string.Empty;

            strSearch = strSearch.Trim().Replace("%')", "");              //"Hunz"
            char[] aSearch = strSearch.ToCharArray();                     //('H','u','n','z')

            for (int y = aSearch.Length - 1; y > 0; --y)
            {
                if (!bDataFound)
                {
                    strSearch = "";
                    for (int z = 0; z <= y; z++)
                        strSearch += aSearch[z];                            //"Hun"

                    strRowFilter = "(Fullname like '%" + strSearch + "%')"; //"(Fullname like '%Hun%')"

                    if (strRowFilter.Length > 0 && !bDataFound)
                    {
                        dN_HLDataSet.tblHL.CaseSensitive = false;  //search (upper/lower) cases
                        dN_HLDataSet.tblHL.DefaultView.RowFilter = strRowFilter;
                        dN_HLDataSet.tblHL.DefaultView.Sort = "HoTen ASC";
                        if (dN_HLDataSet.tblHL.DefaultView.Count > 0)
                        {
                            //"txtsSearch" == (%Hun%) and data found in ["Fullname"], Bind Data Source and return results
                            tblHLBindingSource.DataSource = dN_HLDataSet.tblHL;
                            tblHLBindingSource.Position = 0;
                            datasGridViewBinding(tblHLBindingSource);
                            bDataFound = true;
                            break;
                        }
                    }
                }
                else
                    break;
            }

            return bDataFound;
        }
        /// <summary>
        /// 1st. Search by the last item in the astrSearchName
        /// If Data is found, then filter the result by the rest of the astrSearchName.
        /// If Data is NOT found, 2. Search by the last item in the astrSearchName by searching one less char and loop until the result is found.
        /// Still, if Data is NOT found, 3. Search by the first item in the astrSearchName by searching one less char every time until the result is found.
        /// </summary>
        /// <param name="astrSearchName"></param>
        /// <returns></returns>
        private bool SearchFullnameArray(string[] astrSearchName)
        {
            bool bDataFound = false;
            //int ix = astrSearchName.Length - 1;
            dN_HLDataSet.tblHL.CaseSensitive = false;                   //search (upper/lower) cases

            for (int ix = astrSearchName.Length - 1; ix >= 0; --ix)     //ix = astrSearchName[ix]
            {
                /// /**************************************************
                /// *  Fullname:    nguyen hoang phuong
                /// *  search name: nguyen phuong
                /// *  astrSearchName:   1. [0]nguyen  [1]phuong
                /// *                    2. [0]hoang   [1]phuong
                /// *                    3. [0]nguyen  [1]hoang  [2]phuong
                /// *  1.  Search by last item in the astrSearchName[]     or  2. Search by last item in the astrSearchName[]
                /// *      ix = 1;        [0]nguyen  [1]phuong                    ix = 1;        [0]hoang   [1]phuong
                /// *      iy = 0;                                                iy = 0;
                /// *      strLastIxName = " phuong"                              strLastIxName = " phuong"
                /// *      strALLIxName  = "nguyen"                               strALLIxName = "hoang"
                /// *      strRowFilter  = "Fullname like '% phuong%"             strRowFilter = "Fullname like '% phuong%"
                /// *  2.  Search by last item in the astrSearchName[]     or  2. Search by last item in the astrSearchName[]
                /// *      ix = 1;        [0]nguyen  [1]phuong                    ix = 1;        [0]hoang   [1]phuong
                /// *      iy = 0;                                                iy = 0;
                /// *      strLastIxName = " phuong"                              strLastIxName = " phuong"
                /// *      strALLIxName  = "nguyen"                               strALLIxName = "hoang"
                /// *      strRowFilter  = "Fullname like '% phuong%"             strRowFilter = "Fullname like '% phuong%"
                /// **************************************************/

                if (!bDataFound)
                {
                    string strALLIxName = "";                           //Fullname like '% nguyen%"  ||  Fullname like '% nguyen hoang%"
                    string strMultipleIxName = "";                      //strALLIxName = " nguyen"   ||  strALLIxName = " nguyen hoang"
                    string strLastIxName = astrSearchName[ix];          //[1] = "phuong"             ||  [2] = phuong
                    string strRowFilter = "";

                    for (int iy = 0; iy < ix; iy++)
                    {                                                                                   //                 1st loop                        loop++
                        strALLIxName += " " + astrSearchName[iy];                                       //strALLIxName = " nguyen"  ||  strALLIxName = " nguyen hoang"
                        if (iy == 0)
                            strMultipleIxName += "Fullname like '% " + astrSearchName[iy] + "%'";       //strMultipleIxName = "Fullname like '% nguyen%"
                        else
                            strMultipleIxName += " OR Fullname like '% " + astrSearchName[iy] + "%'";   //strMultipleIxName = "Fullname like '% nguyen%' OR Fullname like '% hoang%'"
                    }

                    //Set the RowFilter by the last item in the astrSearchName
                    strRowFilter = "Fullname like '% " + strLastIxName + "%'";       //Fullname like '% phuong%'

                    if (strRowFilter.Length > 0 && !bDataFound)
                    {
                        //1st - filter the Search by last item in the astrSearchName
                        dN_HLDataSet.tblHL.DefaultView.RowFilter = strRowFilter;    //Fullname like '% phuong%'

                        if (dN_HLDataSet.tblHL.DefaultView.Count > 0)
                        {
                            //This table has all the data with "Fullname like '% phuong%"
                            dN_HLDataSet.tblHL.DefaultView.Sort = "Fullname ASC";
                            dN_HLDataSet.tblHL.AcceptChanges();

                            //1. copy the 1st filter result to tbSearchResult1
                            //2. then use this table to filter for the sencond or third time 
                            DataTable tbSearchResult1 = dN_HLDataSet.tblHL.DefaultView.ToTable("tblResult1", false, "ID", "HoTen", "PhapDanh", "SinhNgay_DL", "MatNgay_DL", "ViTriHinh", "ViTriCot", "HinhFileNamePath", "Note", "Fullname", "FileNumber", "MatNgay_AL", "InsertDate", "UpdateDate", "FullPhapDanh");

                            if (tbSearchResult1.Rows.Count > 0)
                            {
                                string strCheckingValueOnly = tbSearchResult1.Rows[0]["Fullname"].ToString();

                                //Create a DefaultView for More Searches
                                DataView dvSearchResult2 = tbSearchResult1.DefaultView;
                                DataView dvSearchResult3 = tbSearchResult1.DefaultView;

                                //Fullname like '%nguyen%'  ||  Fullname like '%nguyen hoang%'
                                dvSearchResult2.RowFilter = "Fullname like '%" + strALLIxName.Trim() + "%'"; //2nd - filter the Search by the strALLIxName from the dvSearchResult2

                                if (dvSearchResult2.Count == 0)
                                {
                                    strRowFilter = strMultipleIxName;                                       //3rd - filter the Search by the strMultipleIxName;
                                    dvSearchResult3.RowFilter = strRowFilter;

                                    if (dvSearchResult3.Count > 0)
                                    {
                                        //"Fullname like '% nguyen%' OR Fullname like '% hoang%'"
                                        bDataFound = true;
                                        dvSearchResult3.Sort = "Fullname ASC";
                                        tblHLBindingSource.DataSource = dvSearchResult3;
                                        tblHLBindingSource.Position = 0;
                                        datasGridViewBinding(tblHLBindingSource);
                                    }
                                }
                                else
                                {
                                    //strALLIxName = //Fullname like '%nguyen%'  ||  Fullname like '%nguyen hoang%'
                                    bDataFound = true;
                                    dvSearchResult2.Sort = "Fullname ASC";
                                    tblHLBindingSource.DataSource = dvSearchResult2;
                                    tblHLBindingSource.Position = 0;
                                    datasGridViewBinding(tblHLBindingSource);
                                }
                            }//if (tbSearchResult1.Rows.Count > 0)
                        }
                        else
                        {
                            //2.Search by last item in the astrSearchName with 1 less char. and repeat until result found
                            //(dN_HLDataSet.tblHL.DefaultView.Count == 0)

                            strLastIxName = strLastIxName.Trim().Replace("%')", "");    //"phuonz"
                            char[] aLastSearch = strLastIxName.ToCharArray();               //('p','h','u','o','n','z')

                            for (int y = aLastSearch.Length - 1; y > 0; --y)
                            {
                                if (!bDataFound)
                                {
                                    string strSearch = "";
                                    for (int z = 0; z < y; z++)
                                        strSearch += aLastSearch[z];    //"phuon"

                                    //strRowFilter = "(Fullname like '" + strALLIxName.Trim() + " " + strSearch + "%')";      //"(Fullname like 'Diep The Hun%')"
                                    strRowFilter = "Fullname like '% " + strSearch + "%'";       //Fullname like '%phuon%'

                                    if (strRowFilter.Length > 0 && !bDataFound)
                                    {
                                        dN_HLDataSet.tblHL.DefaultView.RowFilter = strRowFilter;    //filter: 1st time by strLastIxName

                                        if (dN_HLDataSet.tblHL.DefaultView.Count > 0)
                                        {
                                            //This table has all the data with "Fullname like '% phuon%"
                                            dN_HLDataSet.tblHL.DefaultView.Sort = "Fullname ASC";
                                            dN_HLDataSet.tblHL.AcceptChanges();

                                            //1. copy the 1st filter result to tbSearchResult1
                                            //2. then use this table to filter the sencond time
                                            DataTable tbSearchResult1 = dN_HLDataSet.tblHL.DefaultView.ToTable("tblResult1", false, "ID", "HoTen", "PhapDanh", "SinhNgay_DL", "MatNgay_DL", "ViTriHinh", "ViTriCot", "HinhFileNamePath", "Note", "Fullname", "FileNumber", "MatNgay_AL", "InsertDate", "UpdateDate", "FullPhapDanh");

                                            if (tbSearchResult1.Rows.Count > 0)
                                            {
                                                //Create a DefaultView for the More Search
                                                DataView dvSearchResult2 = tbSearchResult1.DefaultView;

                                                strRowFilter = "Fullname like '" + strALLIxName.Trim() + "%'";     //Fullname like nguyezz% || nguyen%

                                                dvSearchResult2.RowFilter = strRowFilter;                            //filter 2nd time from tbSearchResult1

                                                if (dvSearchResult2.Count == 0)
                                                {
                                                    //3.Search by first item in the astrSearchName with 1 less char. and repeat until result found
                                                    strALLIxName = strALLIxName.Trim().Replace("%')", "");    //"nguyezz"
                                                    char[] aFirstSearch = strALLIxName.ToCharArray();           //('n','g','u','y','e','z','z')

                                                    for (int a = aFirstSearch.Length - 1; a > 0; --a)
                                                    {
                                                        if (!bDataFound)
                                                        {
                                                            string strFirstSearch = "";
                                                            for (int z = 0; z < a; z++)
                                                                strFirstSearch += aFirstSearch[z];    //"nguyez"

                                                            strRowFilter = "Fullname like '" + strFirstSearch + "%'";   //Fullname like nguyez%

                                                            dvSearchResult2.RowFilter = strRowFilter;                           //filter 3rd time from tbSearchResult1

                                                            if (dvSearchResult2.Count > 0)
                                                            {
                                                                //This table has all the data with "Fullname like nguye%"
                                                                bDataFound = true;
                                                                dvSearchResult2.Sort = "Fullname ASC";
                                                                tblHLBindingSource.DataSource = dvSearchResult2;
                                                                tblHLBindingSource.Position = 0;
                                                                datasGridViewBinding(tblHLBindingSource);
                                                                break;
                                                            }
                                                        }
                                                        else
                                                            break;
                                                    }
                                                    //for (int y = aFirstSearch.Length - 1; y > 0; --y)
                                                    //strRowFilter = "Fullname like '% " + strALLIxName.Trim() + "%'";  //Fullname like % nguyen%
                                                    //dvSearchResult2.RowFilter = strRowFilter;                           //filter 3rd time from tbSearchResult1

                                                    //if (dvSearchResult2.Count == 0)
                                                    //{

                                                    //}
                                                    //else
                                                    //{
                                                    //    //Fullname like %nguyen%  ...  % phuon%
                                                    //    bDataFound = true;
                                                    //    dvSearchResult2.Sort = "Fullname ASC";
                                                    //    tblHLBindingSource.DataSource = dvSearchResult2;
                                                    //    datasGridView.DataSource = tblHLBindingSource;
                                                    //    datasGridView.Focus();
                                                    //}
                                                }
                                                else
                                                {
                                                    //Fullname like nguyen%  ...  % phuon%
                                                    bDataFound = true;
                                                    dvSearchResult2.Sort = "Fullname ASC";
                                                    tblHLBindingSource.DataSource = dvSearchResult2;
                                                    tblHLBindingSource.Position = 0;
                                                    datasGridViewBinding(tblHLBindingSource);
                                                }
                                            }
                                        }
                                    }
                                }
                                else
                                    break;
                            }//for (int y = aSearch.Length - 1; y > 0; --y)

                            if (!bDataFound)
                            {
                                //3.Search by first item in the astrSearchName with 1 less char. and repeat until result found
                                strALLIxName = strALLIxName.Trim().Replace("%')", "");    //"nguyez"
                                char[] aFirstSearch = strALLIxName.ToCharArray();           //('n','g','u','y','e','z')

                                for (int y = aFirstSearch.Length - 1; y > 0; --y)
                                {
                                    if (!bDataFound)
                                    {
                                        string strSearch = "";
                                        for (int z = 0; z < y; z++)
                                            strSearch += aFirstSearch[z];    //"nguye"  //the astrSearchName with 1 less char.

                                        strRowFilter = "Fullname like '" + strSearch + "%'";       //Fullname like nguye%

                                        dN_HLDataSet.tblHL.DefaultView.RowFilter = strRowFilter;    //filter: 1st time by strALLIxName

                                        if (dN_HLDataSet.tblHL.DefaultView.Count > 0)
                                        {
                                            //This table has all the data with "Fullname like nguye%"
                                            dN_HLDataSet.tblHL.DefaultView.Sort = "Fullname ASC";
                                            dN_HLDataSet.tblHL.AcceptChanges();
                                            tblHLBindingSource.DataSource = dN_HLDataSet.tblHL;
                                            tblHLBindingSource.Position = 0;
                                            datasGridViewBinding(tblHLBindingSource);
                                            bDataFound = true;
                                            break;
                                        }
                                    }
                                    else
                                        break;
                                }//for (int y = aFirstSearch.Length - 1; y > 0; --y)
                            }

                        }//(dN_HLDataSet.tblHL.DefaultView.Count == 0)

                    }//if (strRowFilter.Length > 0 && !bDataFound)

                }
                else
                    break;
            }
            return bDataFound;
        }
        private bool SearchFullnameArray_Orig(string[] astrSearchName)
        {
            bool bDataFound = false;
            //int ix = astrSearchName.Length - 1;
            dN_HLDataSet.tblHL.CaseSensitive = false;                   //search (upper/lower) cases

            for (int ix = astrSearchName.Length - 1; ix >= 0; --ix)     //ix = astrSearchName[ix]
            {
                /// /**************************************************
                /// *  Fullname:    nguyen hoang phuong
                /// *  search name: nguyen phuong
                /// *  astrSearchName:   1. [0]nguyen  [1]phuong
                /// *                    2. [0]hoang   [1]phuong
                /// *                    3. [0]nguyen  [1]hoang  [2]phuong
                /// *  1.  Search by last item in the astrSearchName[]     or  2. Search by last item in the astrSearchName[]
                /// *      ix = 1;        [0]nguyen  [1]phuong                    ix = 1;        [0]hoang   [1]phuong
                /// *      iy = 0;                                                iy = 0;
                /// *      strLastIxName = " phuong"                              strLastIxName = " phuong"
                /// *      strALLIxName  = "nguyen"                               strALLIxName = "hoang"
                /// *      strRowFilter  = "Fullname like '% phuong%"             strRowFilter = "Fullname like '% phuong%"
                /// *  1.  Search by last item in the astrSearchName[]     or  2. Search by last item in the astrSearchName[]
                /// *      ix = 1;        [0]nguyen  [1]phuong                    ix = 1;        [0]hoang   [1]phuong
                /// *      iy = 0;                                                iy = 0;
                /// *      strLastIxName = " phuong"                              strLastIxName = " phuong"
                /// *      strALLIxName  = "nguyen"                               strALLIxName = "hoang"
                /// *      strRowFilter  = "Fullname like '% phuong%"             strRowFilter = "Fullname like '% phuong%"
                /// **************************************************/

                if (!bDataFound)
                {
                    string strALLIxName = "";                           //Fullname like '% nguyen%"  ||  Fullname like '% nguyen hoang%"
                    string strMultipleIxName = "";                      //strALLIxName = " nguyen"   ||  strALLIxName = " nguyen hoang"
                    string strLastIxName = astrSearchName[ix];          //[1] = "phuong"             ||  [2] = phuong
                    string strRowFilter = "";

                    for (int iy = 0; iy < ix; iy++)
                    {                                                                                   //                 1st loop                        loop++
                        strALLIxName += " " + astrSearchName[iy];                                       //strALLIxName = " nguyen"  ||  strALLIxName = " nguyen hoang"
                        if (iy == 0)
                            strMultipleIxName += "Fullname like '% " + astrSearchName[iy] + "%'";       //strMultipleIxName = "Fullname like '% nguyen%"
                        else
                            strMultipleIxName += " OR Fullname like '% " + astrSearchName[iy] + "%'";   //strMultipleIxName = "Fullname like '% nguyen%' OR Fullname like '% hoang%'"
                    }

                    //Set the RowFilter by the last item in the astrSearchName
                    strRowFilter = "Fullname like '% " + strLastIxName + "%'";       //Fullname like '% phuong%'

                    if (strRowFilter.Length > 0 && !bDataFound)
                    {
                        //1st - filter the Search by last item in the astrSearchName
                        dN_HLDataSet.tblHL.DefaultView.RowFilter = strRowFilter;    //Fullname like '% phuong%'

                        if (dN_HLDataSet.tblHL.DefaultView.Count > 0)
                        {
                            //This table has all the data with "Fullname like '% phuong%"
                            dN_HLDataSet.tblHL.DefaultView.Sort = "Fullname ASC";
                            dN_HLDataSet.tblHL.AcceptChanges();

                            //1. copy the 1st filter result to tbSearchResult1
                            //2. then use this table to filter for the sencond or third time 
                            DataTable tbSearchResult1 = dN_HLDataSet.tblHL.DefaultView.ToTable("tblResult1", false, "ID", "HoTen", "PhapDanh", "SinhNgay_DL", "MatNgay_DL", "ViTriHinh", "ViTriCot", "HinhFileNamePath", "Note", "Fullname", "FileNumber", "MatNgay_AL", "InsertDate", "UpdateDate", "FullPhapDanh");

                            if (tbSearchResult1.Rows.Count > 0)
                            {
                                string strCheckingValueOnly = tbSearchResult1.Rows[0]["Fullname"].ToString();

                                //Create a DefaultView for More Searches
                                DataView dvSearchResult2 = tbSearchResult1.DefaultView;
                                DataView dvSearchResult3 = tbSearchResult1.DefaultView;

                                dvSearchResult2.RowFilter = strMultipleIxName;                              //2nd - filter the Search by the strMultipleIxName from the dvSearchResult2

                                if (dvSearchResult2.Count == 0)
                                {
                                    strRowFilter = "Fullname like '%" + strALLIxName.Trim() + "%'";         //3rd - filter the Search by the strALLIxName; Fullname like '%nguyen%'  ||  Fullname like '%nguyen hoang%'
                                    dvSearchResult3.RowFilter = strRowFilter;                               //filter 3rd time from tbSearchResult1

                                    if (dvSearchResult3.Count > 0)
                                    {
                                        //Fullname like %nguyen%  ...  % phuong%
                                        bDataFound = true;
                                        dvSearchResult3.Sort = "Fullname ASC";
                                        tblHLBindingSource.DataSource = dvSearchResult3;
                                        tblHLBindingSource.Position = 0;
                                        datasGridViewBinding(tblHLBindingSource);
                                    }
                                }
                                else
                                {
                                    //strMultipleIxName = "Fullname like '% nguyen%' OR Fullname like '% hoang%'"
                                    bDataFound = true;
                                    dvSearchResult2.Sort = "Fullname ASC";
                                    tblHLBindingSource.DataSource = dvSearchResult2;
                                    tblHLBindingSource.Position = 0;
                                    datasGridViewBinding(tblHLBindingSource);
                                }
                            }//if (tbSearchResult1.Rows.Count > 0)
                        }
                        else
                        {
                            //2.Search by last item in the astrSearchName with 1 less char. and repeat until result found
                            //(dN_HLDataSet.tblHL.DefaultView.Count == 0)

                            strLastIxName = strLastIxName.Trim().Replace("%')", "");    //"phuonz"
                            char[] aLastSearch = strLastIxName.ToCharArray();               //('p','h','u','o','n','z')

                            for (int y = aLastSearch.Length - 1; y > 0; --y)
                            {
                                if (!bDataFound)
                                {
                                    string strSearch = "";
                                    for (int z = 0; z < y; z++)
                                        strSearch += aLastSearch[z];    //"phuon"

                                    //strRowFilter = "(Fullname like '" + strALLIxName.Trim() + " " + strSearch + "%')";      //"(Fullname like 'Diep The Hun%')"
                                    strRowFilter = "Fullname like '% " + strSearch + "%'";       //Fullname like '%phuon%'

                                    if (strRowFilter.Length > 0 && !bDataFound)
                                    {
                                        dN_HLDataSet.tblHL.DefaultView.RowFilter = strRowFilter;    //filter: 1st time by strLastIxName

                                        if (dN_HLDataSet.tblHL.DefaultView.Count > 0)
                                        {
                                            //This table has all the data with "Fullname like '% phuon%"
                                            dN_HLDataSet.tblHL.DefaultView.Sort = "Fullname ASC";
                                            dN_HLDataSet.tblHL.AcceptChanges();

                                            //1. copy the 1st filter result to tbSearchResult1
                                            //2. then use this table to filter the sencond time
                                            DataTable tbSearchResult1 = dN_HLDataSet.tblHL.DefaultView.ToTable("tblResult1", false, "ID", "HoTen", "PhapDanh", "SinhNgay_DL", "MatNgay_DL", "ViTriHinh", "ViTriCot", "HinhFileNamePath", "Note", "Fullname", "FileNumber", "MatNgay_AL", "InsertDate", "UpdateDate", "FullPhapDanh");

                                            if (tbSearchResult1.Rows.Count > 0)
                                            {
                                                //Create a DefaultView for the More Search
                                                DataView dvSearchResult2 = tbSearchResult1.DefaultView;

                                                strRowFilter = "Fullname like '" + strALLIxName.Trim() + "%'";     //Fullname like nguyezz% || nguyen%

                                                dvSearchResult2.RowFilter = strRowFilter;                            //filter 2nd time from tbSearchResult1

                                                if (dvSearchResult2.Count == 0)
                                                {
                                                    //3.Search by first item in the astrSearchName with 1 less char. and repeat until result found
                                                    strALLIxName = strALLIxName.Trim().Replace("%')", "");    //"nguyezz"
                                                    char[] aFirstSearch = strALLIxName.ToCharArray();           //('n','g','u','y','e','z','z')

                                                    for (int a = aFirstSearch.Length - 1; a > 0; --a)
                                                    {
                                                        if (!bDataFound)
                                                        {
                                                            string strFirstSearch = "";
                                                            for (int z = 0; z < a; z++)
                                                                strFirstSearch += aFirstSearch[z];    //"nguyez"

                                                            strRowFilter = "Fullname like '" + strFirstSearch + "%'";   //Fullname like nguyez%

                                                            dvSearchResult2.RowFilter = strRowFilter;                           //filter 3rd time from tbSearchResult1

                                                            if (dvSearchResult2.Count > 0)
                                                            {
                                                                //This table has all the data with "Fullname like nguye%"
                                                                bDataFound = true;
                                                                dvSearchResult2.Sort = "Fullname ASC";
                                                                tblHLBindingSource.DataSource = dvSearchResult2;
                                                                tblHLBindingSource.Position = 0;
                                                                datasGridViewBinding(tblHLBindingSource);
                                                                break;
                                                            }
                                                        }
                                                        else
                                                            break;
                                                    }
                                                    //for (int y = aFirstSearch.Length - 1; y > 0; --y)
                                                    //strRowFilter = "Fullname like '% " + strALLIxName.Trim() + "%'";  //Fullname like % nguyen%
                                                    //dvSearchResult2.RowFilter = strRowFilter;                           //filter 3rd time from tbSearchResult1

                                                    //if (dvSearchResult2.Count == 0)
                                                    //{

                                                    //}
                                                    //else
                                                    //{
                                                    //    //Fullname like %nguyen%  ...  % phuon%
                                                    //    bDataFound = true;
                                                    //    dvSearchResult2.Sort = "Fullname ASC";
                                                    //    tblHLBindingSource.DataSource = dvSearchResult2;
                                                    //    datasGridView.DataSource = tblHLBindingSource;
                                                    //    datasGridView.Focus();
                                                    //}
                                                }
                                                else
                                                {
                                                    //Fullname like nguyen%  ...  % phuon%
                                                    bDataFound = true;
                                                    dvSearchResult2.Sort = "Fullname ASC";
                                                    tblHLBindingSource.DataSource = dvSearchResult2;
                                                    tblHLBindingSource.Position = 0;
                                                    datasGridViewBinding(tblHLBindingSource);
                                                }
                                            }
                                        }
                                    }
                                }
                                else
                                    break;
                            }//for (int y = aSearch.Length - 1; y > 0; --y)

                            if (!bDataFound)
                            {
                                //3.Search by first item in the astrSearchName with 1 less char. and repeat until result found
                                strALLIxName = strALLIxName.Trim().Replace("%')", "");    //"nguyez"
                                char[] aFirstSearch = strALLIxName.ToCharArray();           //('n','g','u','y','e','z')

                                for (int y = aFirstSearch.Length - 1; y > 0; --y)
                                {
                                    if (!bDataFound)
                                    {
                                        string strSearch = "";
                                        for (int z = 0; z < y; z++)
                                            strSearch += aFirstSearch[z];    //"nguye"  //the astrSearchName with 1 less char.

                                        strRowFilter = "Fullname like '" + strSearch + "%'";       //Fullname like nguye%

                                        dN_HLDataSet.tblHL.DefaultView.RowFilter = strRowFilter;    //filter: 1st time by strALLIxName

                                        if (dN_HLDataSet.tblHL.DefaultView.Count > 0)
                                        {
                                            //This table has all the data with "Fullname like nguye%"
                                            dN_HLDataSet.tblHL.DefaultView.Sort = "Fullname ASC";
                                            dN_HLDataSet.tblHL.AcceptChanges();
                                            tblHLBindingSource.DataSource = dN_HLDataSet.tblHL;
                                            tblHLBindingSource.Position = 0;
                                            datasGridViewBinding(tblHLBindingSource);
                                            bDataFound = true;
                                            break;
                                        }
                                    }
                                    else
                                        break;
                                }//for (int y = aFirstSearch.Length - 1; y > 0; --y)
                            }

                        }//(dN_HLDataSet.tblHL.DefaultView.Count == 0)

                    }//if (strRowFilter.Length > 0 && !bDataFound)

                }
                else
                    break;
            }
            return bDataFound;
        }
        private bool SearchPhapDanhAsIs(string strSearch)
        {
            bool bDataFound = false;
            string strRowFilter = string.Empty;

            strSearch = strSearch.Trim().Replace("%')", "");              //"Hunz"
            char[] aSearch = strSearch.ToCharArray();                     //('H','u','n','z')

            for (int y = aSearch.Length - 1; y > 0; --y)
            {
                if (!bDataFound)
                {
                    strSearch = "";
                    for (int z = 0; z <= y; z++)
                        strSearch += aSearch[z];                            //"Hun"

                    strRowFilter = "(FullPhapDanh like '%" + strSearch + "%')"; //"(FullPhapDanh like '%Hun%')"

                    if (strRowFilter.Length > 0 && !bDataFound)
                    {
                        dN_HLDataSet.tblHL.CaseSensitive = false;  //search (upper/lower) cases
                        dN_HLDataSet.tblHL.DefaultView.RowFilter = strRowFilter;
                        dN_HLDataSet.tblHL.DefaultView.Sort = "HoTen ASC";
                        if (dN_HLDataSet.tblHL.DefaultView.Count > 0)
                        {
                            //"txtsSearch" == (%Hun%) and data found in ["FullPhapDanh"], Bind Data Source and return results
                            tblHLBindingSource.DataSource = dN_HLDataSet.tblHL;
                            tblHLBindingSource.Position = 0;
                            datasGridViewBinding(tblHLBindingSource);
                            bDataFound = true;
                            break;
                        }
                    }
                }
                else
                    break;
            }

            return bDataFound;
        }
        /// <summary>
        /// 1. Search by the last item in the astrSearchName
        /// If Data is found, then filter the result by the rest of the astrSearchName.
        /// If Data is NOT found, 2. Search by the last item in the astrSearchName by searching one less char and loop until the result is found.
        /// Still, if Data is NOT found, 3. Search by the first item in the astrSearchName by searching one less char every time until the result is found.
        /// </summary>
        /// <param name="astrSearchName"></param>
        /// <returns></returns>
        private bool SearchPhapDanhArray(string[] astrSearchName)
        {
            bool bDataFound = false;

            dN_HLDataSet.tblHL.CaseSensitive = false;                   //search (upper/lower) cases

            for (int ix = astrSearchName.Length - 1; ix >= 0; --ix)
            {
                /**********************************************
                 *  Phap Danh:   Kim Ngoc Quy Nhơn
                 *  search name: Kim Ngoc
                 *  astrSearchName:   [0]kim  [1]ngoc
                 *  
                 *  1. Search by last item in the astrSearchName
                 *      ix = 1;     //astrSearchName.Length - 1;
                 *      iy = 0;
                 *      strLastIxName = " ngoc"
                 *      strALLIxName = "kim"
                 *      strRowFilter = "FullPhapDanh like '% ngoc%"
                 **********************************************/
                if (!bDataFound)
                {
                    string strALLIxName = "";
                    string strRowFilter = "";
                    string strLastIxName = astrSearchName[ix];          //[1]ngoc

                    for (int iy = 0; iy < ix; iy++)
                        strALLIxName += " " + astrSearchName[iy];     //[0]kim

                    strRowFilter = "FullPhapDanh like '% " + strLastIxName + "%'";       //set filter = strLastIxSearch;  FullPhapDanh like '% ngoc%'

                    if (strRowFilter.Length > 0 && !bDataFound)
                    {
                        dN_HLDataSet.tblHL.DefaultView.RowFilter = strRowFilter;    //filter: 1st time by LastIxSearch
                        dN_HLDataSet.tblHL.DefaultView.Sort = "FullPhapDanh ASC";

                        if (dN_HLDataSet.tblHL.DefaultView.Count > 0)
                        {
                            //1. Search by last item in the astrSearchName
                            //This table has all the data with "FullPhapDanh like '% ngoc%"
                            dN_HLDataSet.tblHL.AcceptChanges();

                            //1. copy the 1st filter result to tbSearchResult1
                            //2. then use this table to filter the sencond time 
                            DataTable tbSearchResult1 = dN_HLDataSet.tblHL.DefaultView.ToTable("tblResult1", false, "ID", "HoTen", "PhapDanh", "SinhNgay_DL", "MatNgay_DL", "ViTriHinh", "ViTriCot", "HinhFileNamePath", "Note", "Fullname", "FileNumber", "MatNgay_AL", "InsertDate", "UpdateDate", "FullPhapDanh");

                            if (tbSearchResult1.Rows.Count > 0)
                            {
                                string strCheckingValueOnly = tbSearchResult1.Rows[0]["FullPhapDanh"].ToString();

                                //Create a DefaultView for the More Search
                                DataView dvSearchResult2 = tbSearchResult1.DefaultView;

                                strRowFilter = "FullPhapDanh like '" + strALLIxName.Trim() + "%'";    //FullPhapDanh like 'kim%'

                                dvSearchResult2.RowFilter = strRowFilter;                           //filter 2nd time from tbSearchResult1

                                if (dvSearchResult2.Count == 0)
                                {
                                    strRowFilter = "FullPhapDanh like '%" + strALLIxName.Trim() + "%'";   //FullPhapDanh like '%kim%'
                                    dvSearchResult2.RowFilter = strRowFilter;                           //filter 3rd time from tbSearchResult1

                                    if (dvSearchResult2.Count > 0)
                                    {
                                        //FullPhapDanh like %kim%  ...  % ngoc%
                                        bDataFound = true;
                                        dvSearchResult2.Sort = "FullPhapDanh ASC";
                                        tblHLBindingSource.DataSource = dvSearchResult2;
                                        tblHLBindingSource.Position = 0;
                                        datasGridViewBinding(tblHLBindingSource);
                                    }
                                }
                                else
                                {
                                    //FullPhapDanh like kim%  ...  % ngoc%
                                    bDataFound = true;
                                    dvSearchResult2.Sort = "FullPhapDanh ASC";
                                    tblHLBindingSource.DataSource = dvSearchResult2;
                                    tblHLBindingSource.Position = 0;
                                    datasGridViewBinding(tblHLBindingSource);
                                }
                            }//if (tbSearchResult1.Rows.Count > 0)
                        }
                        else
                        {
                            //2.Search by last item in the astrSearchName with 1 less char. and repeat until result found
                            //(dN_HLDataSet.tblHL.DefaultView.Count == 0)

                            strLastIxName = strLastIxName.Trim().Replace("%')", "");    //"ngoz"
                            char[] aLastSearch = strLastIxName.ToCharArray();               //('p','h','u','o','n','z')

                            for (int y = aLastSearch.Length - 1; y > 0; --y)
                            {
                                if (!bDataFound)
                                {
                                    string strSearch = "";
                                    for (int z = 0; z < y; z++)
                                        strSearch += aLastSearch[z];    //"ngo"

                                    strRowFilter = "FullPhapDanh like '% " + strSearch + "%'";       //FullPhapDanh like '%ngo%'

                                    if (strRowFilter.Length > 0 && !bDataFound)
                                    {
                                        dN_HLDataSet.tblHL.DefaultView.RowFilter = strRowFilter;    //filter: 1st time by strLastIxName

                                        if (dN_HLDataSet.tblHL.DefaultView.Count > 0)
                                        {
                                            //This table has all the data with "FullPhapDanh like '% ngo%"
                                            dN_HLDataSet.tblHL.DefaultView.Sort = "FullPhapDanh ASC";
                                            dN_HLDataSet.tblHL.AcceptChanges();

                                            //1. copy the 1st filter result to tbSearchResult1
                                            //2. then use this table to filter the sencond time
                                            DataTable tbSearchResult1 = dN_HLDataSet.tblHL.DefaultView.ToTable("tblResult1", false, "ID", "HoTen", "PhapDanh", "SinhNgay_DL", "MatNgay_DL", "ViTriHinh", "ViTriCot", "HinhFileNamePath", "Note", "Fullname", "FileNumber", "MatNgay_AL", "InsertDate", "UpdateDate", "FullPhapDanh");

                                            if (tbSearchResult1.Rows.Count > 0)
                                            {
                                                //Create a DefaultView for the More Search
                                                DataView dvSearchResult2 = tbSearchResult1.DefaultView;

                                                strRowFilter = "FullPhapDanh like '" + strALLIxName.Trim() + "%'";     //FullPhapDanh like kizz% || kim%

                                                dvSearchResult2.RowFilter = strRowFilter;                            //filter 2nd time from tbSearchResult1

                                                if (dvSearchResult2.Count == 0)
                                                {
                                                    //3.Search by first item in the astrSearchName with 1 less char. and repeat until result found
                                                    strALLIxName = strALLIxName.Trim().Replace("%')", "");    //"kizz"
                                                    char[] aFirstSearch = strALLIxName.ToCharArray();           //('k','i','z','z')

                                                    for (int a = aFirstSearch.Length - 1; a > 0; --a)
                                                    {
                                                        if (!bDataFound)
                                                        {
                                                            string strFirstSearch = "";
                                                            for (int z = 0; z < a; z++)
                                                                strFirstSearch += aFirstSearch[z];    //"nguyez"

                                                            strRowFilter = "FullPhapDanh like '" + strFirstSearch + "%'";   //FullPhapDanh like kiz%

                                                            dvSearchResult2.RowFilter = strRowFilter;                           //filter 3rd time from tbSearchResult1

                                                            if (dvSearchResult2.Count > 0)
                                                            {
                                                                //This table has all the data with "FullPhapDanh like ki%"
                                                                bDataFound = true;
                                                                dvSearchResult2.Sort = "FullPhapDanh ASC";
                                                                tblHLBindingSource.DataSource = dvSearchResult2;
                                                                tblHLBindingSource.Position = 0;
                                                                datasGridViewBinding(tblHLBindingSource);
                                                                break;
                                                            }
                                                        }
                                                        else
                                                            break;
                                                    }
                                                }
                                                else
                                                {
                                                    //FullPhapDanh like ki%  ...  % ngo%
                                                    bDataFound = true;
                                                    dvSearchResult2.Sort = "FullPhapDanh ASC";
                                                    tblHLBindingSource.DataSource = dvSearchResult2;
                                                    tblHLBindingSource.Position = 0;
                                                    datasGridViewBinding(tblHLBindingSource);
                                                }
                                            }
                                        }
                                    }
                                }
                                else
                                    break;
                            }//for (int y = aSearch.Length - 1; y > 0; --y)

                            if (!bDataFound)
                            {
                                //3.Search by first item in the astrSearchName with 1 less char. and repeat until result found
                                strALLIxName = strALLIxName.Trim().Replace("%')", "");    //"kiz"
                                char[] aFirstSearch = strALLIxName.ToCharArray();           //('k','i','z')

                                for (int y = aFirstSearch.Length - 1; y > 0; --y)
                                {
                                    if (!bDataFound)
                                    {
                                        string strSearch = "";
                                        for (int z = 0; z < y; z++)
                                            strSearch += aFirstSearch[z];    //"nguye"

                                        strRowFilter = "FullPhapDanh like '" + strSearch + "%'";       //FullPhapDanh like ki%

                                        dN_HLDataSet.tblHL.DefaultView.RowFilter = strRowFilter;    //filter: 1st time by strALLIxName

                                        if (dN_HLDataSet.tblHL.DefaultView.Count > 0)
                                        {
                                            //This table has all the data with "FullPhapDanh like ki%"
                                            dN_HLDataSet.tblHL.DefaultView.Sort = "FullPhapDanh ASC";
                                            dN_HLDataSet.tblHL.AcceptChanges();
                                            tblHLBindingSource.DataSource = dN_HLDataSet.tblHL;
                                            tblHLBindingSource.Position = 0;
                                            datasGridViewBinding(tblHLBindingSource);
                                            bDataFound = true;
                                            break;
                                        }
                                    }
                                    else
                                        break;
                                }//for (int y = aFirstSearch.Length - 1; y > 0; --y)
                            }

                        }//(dN_HLDataSet.tblHL.DefaultView.Count == 0)

                    }//if (strRowFilter.Length > 0 && !bDataFound)

                }
                else
                    break;
            }
            return bDataFound;
        }
        private void Search()
        {
            try
            {
                lblsErrorMsg.Text = "";
                string strRowFilter = "";
                dN_HLDataSet.tblHL.CaseSensitive = false;

                if (string.IsNullOrEmpty(txtsSearch.Text.Trim()) && string.IsNullOrEmpty(txtsFNumbSearch.Text.Trim()))
                {
                    //===================================================================================================================
                    //A. Search fields are empty. - Search == ""
                    //===================================================================================================================

                    RefreshSearchTab();
                }
                else
                {
                    if (!string.IsNullOrEmpty(txtsFNumbSearch.Text.Trim()))
                    {
                        //===================================================================================================================
                        //B. Search field: txtsFNumbSearch is not empty. - Search "FileNumber like '1917%'
                        //===================================================================================================================

                        strRowFilter = "FileNumber like '" + txtsFNumbSearch.Text.Trim() + "%'";

                        dN_HLDataSet.tblHL.DefaultView.RowFilter = strRowFilter;

                        if (dN_HLDataSet.tblHL.DefaultView.Count > 0)
                        {
                            dN_HLDataSet.tblHL.DefaultView.Sort = "FileNumber ASC";
                            tblHLBindingSource.DataSource = dN_HLDataSet.tblHL;
                            tblHLBindingSource.Position = 0;
                            datasGridViewBinding(tblHLBindingSource);
                        }
                        else
                        {
                            lblsErrorMsg.Text = "No FileNumber found.";
                        }
                    }
                    else
                    {
                        //===================================================================================================================
                        //C. Search field: The txtsSearch.Text could be a filenumber.
                        //      1. Search by the FileNumber - [txtsSearch.Text == 1917]
                        //          "FileNumber like '" + txtsSearch.Text.Trim() + "%'"
                        //===================================================================================================================
                        strRowFilter = "FileNumber like '" + txtsSearch.Text.Trim() + "%'";

                        dN_HLDataSet.tblHL.DefaultView.RowFilter = strRowFilter;

                        if (dN_HLDataSet.tblHL.DefaultView.Count > 0)
                        {
                            //FileNumber found
                            dN_HLDataSet.tblHL.DefaultView.Sort = "FileNumber ASC";
                            tblHLBindingSource.DataSource = dN_HLDataSet.tblHL;
                            tblHLBindingSource.Position = 0;
                            datasGridViewBinding(tblHLBindingSource);
                        }
                        else
                        {
                            ///===================================================================================================================
                            ///C. Search field: The txtsSearch.Text could be a HL name.
                            ///      2. Search by the Fullname - [txtsSearch.Text == Phuong] || [txtsSearch.Text == Nguyen Phuong]  || [txtsSearch.Text == Nguyen Hoang Phuong]
                            ///          i. Convert the txtsSearch from VN to EN
                            ///          ii. Split the txtsSearch.Text.Trim() into an array
                            ///          iii. If count == 1 then SearchFullnameAsIs() otherwise
                            ///               a. If (cksPhapDanh.Checked) >> SearchPhapDanhAsIs()
                            ///               b. SearchFullnameAsIs()
                            ///          iv.  If count > 1
                            ///               a. If (cksPhapDanh.Checked) >> SearchPhapDanhArray()
                            ///               b. SearchFullnameArray()
                            ///===================================================================================================================

                            string strSearchEnglish = Convert_VN_To_Eng(txtsSearch.Text.Trim());    //i.

                            string[] astrSearch = strSearchEnglish.Split(' ');                      //ii.

                            if (astrSearch.Length == 1)
                            {
                                //iii.      count == 1                                              [txtsSearch.Text == Phuong]
                                if (cksPhapDanh.Checked)
                                {
                                    if (!SearchPhapDanhAsIs(astrSearch[0]))                         //iii.a.
                                        lblsErrorMsg.Text = "No record found.";
                                }
                                else
                                {
                                    if (!SearchFullnameAsIs(astrSearch[0]))                         //iii.b.
                                        lblsErrorMsg.Text = "No record found.";
                                }
                            }
                            else
                            {
                                //iv.       count > 1                                               [txtsSearch.Text == Nguyen Phuong]     [txtsSearch.Text == Nguyen Hoang Phuong]
                                if (cksPhapDanh.Checked)
                                {
                                    if (!SearchPhapDanhArray(astrSearch))                           //iv.a.
                                        lblsErrorMsg.Text = "No record found.";
                                }
                                else
                                {
                                    if (!SearchFullnameArray(astrSearch))                           //iv.b.
                                        lblsErrorMsg.Text = "No record found.";
                                }
                            }
                        }
                    }
                }

                DisplayImage();                         //last call for DisplayImage() Search
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Message", MessageBoxButtons.OK, MessageBoxIcon.Error);
                tblHLBindingSource.ResetBindings(false);
            }
        }
        private void txtsFNumbSearch_MouseClick(object sender, MouseEventArgs e)
        {
            ClearAllSearchFields();
        }
        private void txtsFNumbSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Home) //if (e.KeyCode == Keys.Back)
            {
                ClearAllSearchFields();
            }
        }
        private void txtsFNumbSearch_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)13)   //Enter key press
            {
                Search();
            }
        }
        private void ckPhapDanh_Click(object sender, EventArgs e)
        {
            Search();
        }
        private void txtsViTriHinh_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)13)   //Enter key press
            {
                SaveEdit();
            }
        }
        private void txtsViTriCot_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)13)  //Enter key press
                SaveEdit();             //Save this record to the DB when the user hits the "Enter" key in Cot Location textBox.
        }
        private void txtsHoTen_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)13)   //Enter key press
            {
                SaveEdit();
            }
        }
        private void txtsPhapDanh_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)13)   //Enter key press
            {
                SaveEdit();
            }
        }
        private void txtsSinh_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)13)   //Enter key press
            {
                SaveEdit();
            }
        }
        private void txtsTu_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)13)   //Enter key press
            {
                SaveEdit();
            }
        }
        private void txtsTuAL_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)13)   //Enter key press
            {
                SaveEdit();
            }
        }
        private void txtsFilename_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)13)   //Enter key press
            {
                SaveEdit();
            }
        }
        private void lblsOrigFilename_TextChanged(object sender, EventArgs e)
        {
            //When datasGridView Refresh with new updated data
            if (lblsOrigFilename.Text != "")
            {
                DisplayImage();
            }
        }
        #endregion - Search Tab Section

        /********************************************* TAB CONTROLS SECTION **********************************************/

        /// <summary>
        /// SWITCH THE TABS Events
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tabControl1_Click(object sender, EventArgs e)
        {
            if (tabControl1.SelectedIndex == 0)         //Search Tab
            {
                RefreshSearchTab();
            }
            else if (tabControl1.SelectedIndex == 1)    //Insert Tab
            {
                BindingImgInsertTab();
            }
        }

        /// <summary>
        /// When TabPage.Selected index changes, then change the Tab Header Font to Bold and Red
        /// in the "Properties" pane - SET the "DrawMode" = "OwnerDrawFixed" FOR THIS EVENT TO FIRE!!
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tabControl1_DrawItem(object sender, DrawItemEventArgs e)
        {
            //http://vbcity.com/blogs/xtab/archive/2014/09/14/windows-forms-how-to-bold-the-header-of-a-selected-tab.aspx
            //The Windows Form
            //Start by dragging a TabControl onto the Windows Form.
            //Then in the "Properties" pane for the TabControl find the "DrawMode" property and change it to "OwnerDrawFixed"
            // --------------- MUST READ THIS SECTION FOR THE "tabControl1_DrawItem" EVENT TO FIRE!!


            //http://vbcity.com/blogs/xtab/archive/2014/09/16/tabcontrol-how-to-change-color-and-size-of-the-selected-tab.aspx

            //Identify which TabPage is currently selected
            TabPage tabPageSelected = tabControl1.TabPages[e.Index];

            //Get the area of the header of this TabPage
            Rectangle headerRect = tabControl1.GetTabRect(e.Index);

            //Create two Brushes to paint the Text
            SolidBrush blackTextBrush = new SolidBrush(Color.Black);
            SolidBrush redTextBrush = new SolidBrush(Color.Red);

            //Set the Alignment of the Text
            StringFormat strFmt = new StringFormat();
            strFmt.Alignment = StringAlignment.Center;
            strFmt.LineAlignment = StringAlignment.Center;

            //Paint the Text using the appropriate Bold and Color setting
            if (Convert.ToBoolean(e.State) && Convert.ToBoolean(DrawItemState.Selected))
            {
                Font boldFont = new Font(tabControl1.Font.Name, tabControl1.Font.Size, FontStyle.Bold);
                e.Graphics.DrawString(tabPageSelected.Text, boldFont, redTextBrush, headerRect, strFmt);
            }
            else
                e.Graphics.DrawString(tabPageSelected.Text, e.Font, blackTextBrush, headerRect, strFmt);

            //Job is done: dispose of the brushes
            blackTextBrush.Dispose();
            redTextBrush.Dispose();
        }

        /****************************************** END TAB CONTROLS SECTION *********************************************/

        #region - Notes Sections

        //DateTime.Now.ToString("G") : 08/17/2000 16:32:32
        // d :08/17/2000
        // D :Thursday, August 17, 2000
        // f :Thursday, August 17, 2000 16:32
        // F :Thursday, August 17, 2000 16:32:32
        // g :08/17/2000 16:32
        // G :08/17/2000 16:32:32
        // m :August 17
        // r :Thu, 17 Aug 2000 23:32:32 GMT
        // s :2000-08-17T16:32:32
        // t :16:32
        // T :16:32:32
        // u :2000-08-17 23:32:32Z
        // U :Thursday, August 17, 2000 23:32:32
        // y :August, 2000
        // dddd, MMMM dd yyyy :Thursday, August 17 2000
        // ddd, MMM d "'"yy :Thu, Aug 17 '00
        // dddd, MMMM dd :Thursday, August 17
        // M/yy :8/00
        // dd-MM-yy :17-08-00

        /* To set Tab Order of controls on FORM */
        // On the FORM Design, click the VIEW menu >> select Tab Order
        // then Click the control
        // https://docs.microsoft.com/en-us/dotnet/desktop/winforms/controls/how-to-set-the-tab-order-on-windows-forms?view=netframeworkdesktop-4.8

        /* Add a TabControl to a FORM */
        // On the Design tab, in the Controls group, click the TabControl tool.
        // This activates the tab-order selection mode on the form.
        // Click the controls sequentially to establish the tab order you want.
        // https://support.microsoft.com/en-us/office/create-a-tabbed-form-6869dee9-3ab7-4f3d-8e65-3a84183c9815#bm4

        /* Add/Remove a TabControl.TabPage Control to a FORM */
        // Select TabControl on a FORM
        // Right click and select Add Tab or Remove Tab

        /* Reorder TabPages on a FORM */
        // 1. Right-click a tab, or right-click the blank area at the top of the tab control.
        // 2. Click Page Order.
        // 3. In the Page Order dialog box, select the page that you want to move.
        // 4. Click Move Up or Move Down to place the page in the order you want.
        // 5. Repeat steps 3 and 4 for any other pages that you want to move.

        /* Move existing Form's controls to a tab page */
        // Drag the Form's control on to the TabPage for it to bind to the TabPage 

        /* tabcontrol resize with form */
        //Use the Anchor property. Anchor the tab control to all 4 edges of the form.

        #endregion - Notes Sections        

        #region - Insert Tab Section
        private void BindingImgInsertTab()
        {
            try
            {
                if (lstiBoxHLImg.Items.Count > 0)
                    lstiBoxHLImg.Items.Clear();

                //#1. get the list of All HL image files
                ArrayList aLstHLImg = Util.SearchFileName(_ImgFolderPath, "*.jpg|*.png");

                //DO NOT use lstBoxHLImg.DataSource = aLstHLImg;
                //because you can't use lstBoxHLImg.Items.RemoveAt(ixSelected);
                foreach (string strHL_Img in aLstHLImg)
                {
                    lstiBoxHLImg.Items.Add(strHL_Img);          //MUST ADD EACH IMG to the Items list: lstBoxHLImg.Items.Add(strHL_Img) IN ORDER FOR: lstBoxHLImg.Items.RemoveAt(ixSelected);    TO WORK!!
                }

                if (lstiBoxHLImg.Items.Count > 0)
                    lstiBoxHLImg.SelectedIndex = 0;             //trigger lstiBoxHLImg_SelectedIndexChanged
                else
                    lstiBoxHLImg.Items.Add("No HL Image Found.");

            }
            catch (Exception ex)
            {
                lbliErrorMsg.Text = ex.ToString();
            }
        }
        /******************************************************************************************************************/
        private void lstiBoxHLImg_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                _ixSelectLength = 0;    //reset when SelectedIndexChanged
                _iClickCount = 0;       //reset when SelectedIndexChanged
                ClearInsertPicBox();

                txtiViTriHinh.Text = "";
                txtiViTriCot.Text = "";
                txtiFileNumber.Text = "";
                txtiHoTen.Text = "";
                txtiPhapDanh.Text = "";
                txtiSinh.Text = "";
                txtiTu.Text = "";
                txtiTuAL.Text = "";
                txtiNote.Text = "";
                lbliFullname.Text = "";
                lbliFullPhapDanh.Text = "";
                btniSave.Visible = true;
                piciBoxHLDup.Visible = false;
                txtiDupImgFilename.Visible = false;

                if (lstiBoxHLImg.SelectedIndex >= 0)
                {
                    txtiFilename.Text = lstiBoxHLImg.SelectedItem.ToString();
                    lbliOrigFilename.Text = lstiBoxHLImg.SelectedItem.ToString();
                    txtiFilenameParsing.Text = lstiBoxHLImg.SelectedItem.ToString().Replace(".jpg", "");
                    txtiFilenameParsing.Enabled = true;

                    DisplayImageInsert();            //first call for DisplayImageInsert()
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Message", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }
        private void DisplayImageInsert()
        {
            lbliErrorMsg.Text = "";

            if (txtiFilename.Text.Trim().Length > 0)
            {
                string strImgFileNamePath = _ImgFolderPath + txtiFilename.Text;

                if (File.Exists(strImgFileNamePath))
                {
                    Bitmap bitImageFile = new Bitmap(strImgFileNamePath);

                    if (piciBoxHL == null)
                    {
                        piciBoxHL = new PictureBox();
                        piciBoxHL.Location = new Point(17, 230);
                    }
                    piciBoxHL.SizeMode = PictureBoxSizeMode.StretchImage;    //in order to have any image "resize" to fit a picturebox, you must set PictureBoxSizeMode.StretchImage 
                    piciBoxHL.Width = 300; // 440;   // 300;   // 580;
                    piciBoxHL.Height = 360; // 450;  // 260;   // 500;
                    piciBoxHL.Image = (Image)bitImageFile;
                    piciBoxHL.Refresh();
                    bitImageFile = null; //to release the image file and allow this image file to be deleted 
                }
                else
                {
                    piciBoxHL.Image = null;  //Hinh Not found.
                    lbliErrorMsg.Text = "HL Image Not Found.";
                }
            }
            else
            {
                piciBoxHL.Image = null;
            }
        }
        private string GetDupHL(string strNewImgFilename, string strNewHL_Fullname)
        {
            string strDupHL = "";
            string strRowFilter = "";   //"(FileName like '%" + strNewImgFilename + "%') OR (Fullname like '%" + (strNewHL_Fullname != "HOA SEN" ? strNewHL_Fullname: strNewImgFilename) + "%')";

            //if (strNewHL_Fullname.ToUpper() == "HOA SEN" || strNewHL_Fullname.ToUpper() == "A DI DA PHAT" || strNewHL_Fullname.ToUpper() == "BAI VI CHU HOA" || strNewHL_Fullname.ToUpper() == "VO DANH")
            //    strRowFilter = "(FileName like '%" + strNewImgFilename + "%') OR (Fullname like '%" + strNewImgFilename + "%')";
            //else

            strRowFilter = "(HinhFileNamePath like '%" + strNewImgFilename + "%') OR (Fullname like '%" + strNewHL_Fullname + "%')";

            dN_HLDataSet.tblHL.CaseSensitive = false;  //search (upper/lower) cases

            dN_HLDataSet.tblHL.DefaultView.RowFilter = strRowFilter;

            DataView dataView = dN_HLDataSet.tblHL.DefaultView;

            if (dataView.Count > 0)
            {
                strDupHL = dataView[0]["HinhFileNamePath"].ToString();
            }

            return strDupHL;
        }
        private void DisplayDupImage(string strDupImgFilename)
        {
            lbliErrorMsg.Text = "";

            if (strDupImgFilename != "")
            {
                txtiDupImgFilename.Visible = true;
                txtiDupImgFilename.Text = strDupImgFilename;
                string strDupImgFileNamePath = _ImgFolderDonePath + strDupImgFilename;

                piciBoxHLDup.Visible = true;

                if (File.Exists(strDupImgFileNamePath))
                {
                    Bitmap bitDupImageFile = new Bitmap(strDupImgFileNamePath);

                    piciBoxHLDup.SizeMode = PictureBoxSizeMode.StretchImage;    //in order to have any image "resize" to fit a picturebox, you must set PictureBoxSizeMode.StretchImage 
                    piciBoxHLDup.Width = 182;
                    piciBoxHLDup.Height = 185;
                    piciBoxHLDup.Image = (Image)bitDupImageFile;
                    piciBoxHLDup.Refresh();
                    bitDupImageFile = null; //to release the image file and allow this image file to be deleted 
                }
                else
                {
                    piciBoxHLDup.Image = null;  //Hinh Not found.
                    lbliErrorMsg.Text = "Duplicate HL Image Not Found.";
                }
            }
            else
            {
                piciBoxHLDup.Image = null;
                piciBoxHLDup.Visible = false;
            }
        }
        private void ClearInsertPicBox()
        {
            //release Picturebox's image in order for the File.Move() to work!
            if (piciBoxHL.Image != null)
            {
                piciBoxHL.Image.Dispose();
                piciBoxHL.Image = null;
            }
        }
        private void ClearDupPicBox()
        {
            //release Picturebox's Duplicate image in order for the File.Move() to work!
            if (piciBoxHLDup.Image != null)
            {
                txtiDupImgFilename.Visible = false;
                txtiDupImgFilename.Text = "";
                piciBoxHLDup.Image.Dispose();
                piciBoxHLDup.Image = null;
            }

            piciBoxHLDup.Visible = false;
        }
        private List<KeyValuePair<string, string>> SinhTuTuAlFmt(string strSinhTuTuAmLich)
        {
            #region - Test data
            //060 RICHARD DONALD PETERSON - Nov. 15, 1946 - Mar. 23,2009
            //006.1 KARL J. LEMARIE SR. - Oct. 23, 1927 - Apr. 2, 1999
            //006.3 BRENDA LEMARIE - Sept. 17, 1962 - Nov. 20, 2000
            //10.16 NGUYỄN NGỌC HÒA PD Tâm Lộc 28.8.1937 27.10 ÂL
            //033.1 ĐOÀN VIẾT CẢNH PD Trí Thông - 16-10-1933 - 26-5-2006 ÂL
            //033.2 ĐOÀN VIẾT CUNG - 21-5-1962 - 17-11-2001 ÂL
            //033.3 ĐOÀN VIẾT BẢY PD Chinh Lược Tâm 7-5-1944  26-1 Nhâm Thìn
            //046.3 VĂN XUÂN NHỤY PD Nhật Hụê Phước 13-06-1920 30-04 N
            //10.16 NGUYỄN NGỌC HÒA PD Tâm Lộc 28.8.1937 27.10 ÂL
            //10.11 NGUYỄN MAI CHỬNG 1940 14.7 Tân Tỵ
            //050 TRẦN NHẬT NGÂN  PD Nhật Quang 24.11 Nhâm Ngọ  28
            //070.46 HỒ THỊ THU CÚC PD Quảng Hạnh 5.5.1934 8.4 C Tý
            //121.10 ĐỖ NGUYÊN DUNG DIANE PD Diệu Hạnh 6.2.1974 27.2.2018 12.1 M Tuất
            //66.c NGUYỄN THỊ NỠ PD Khánh Hà 1928 9.11.16 10.10 B Thân
            //034 NGUYỄN THỊ CHÂU PD Diệu Bửu - 7-4-1923 - 22-4-2009 ÂL Kỷ Sửu
            //70.46 TÔN NỮ T CAM PD Nhật Thùy 1933 Q Dậu 2.5 GNgọ 30.5.2014
            //036.1 LƯU VĂN MINH PD Quảng Đại - 0 - 7-4 ÂL
            //046.1 ĐOÀN VĂN THÀNH PD Nhật Huệ Niệm - 1915 - 2008
            //035.2 Nguyễn Vô Danh - 1968 - 0
            //015.2 TRẦN VĂN BA - 22-10-ÂL
            //10.10 BỬU VIỆT 1977
            //092.7 NGÔ ĐÌNH SƠN 1974 2005

            // - Nov. 15, 1946 - Mar. 23,2009
            // - Oct. 23, 1927 - Apr. 2, 1999
            // - Sept. 17, 1962 - Nov. 20, 2000
            // - 16-10-1933 - 26-5-2006 ÂL
            // - 7-4-1923 - 22-4-2009 ÂL Kỷ Sửu
            // - 1915 - 2008
            // - 0 - 7-4 ÂL
            // - 22-10-ÂL
            // 28.8.1937 27.10 ÂL
            // 7-5-1944  26-1 Nhâm Thìn
            // 13-06-1920 30-04 N
            // 28.8.1937 27.10 ÂL
            // 1940 14.7 Tân Tỵ
            // 24.11 Nhâm Ngọ  28
            // 1928 9.11.16 10.10 B Thân
            // 1933 Q Dậu 2.5 GNgọ 30.5.2014
            // 5.5.1934 8.4 C Tý
            // 6.2.1974 27.2.2018 12.1 M Tuất
            // 1977
            // 1974 2005

            //Nov. 15, 1946 - Mar. 23,2009
            //Oct. 23, 1927 - Apr. 2, 1999
            //Sept. 17, 1962 - Nov. 20, 2000
            //16-10-1933 - 26-5-2006 ÂL
            //7-4-1923 - 22-4-2009 ÂL Kỷ Sửu
            //1915 - 2008
            //0 - 7-4 ÂL
            //22-10-ÂL
            //28.8.1937 27.10 ÂL
            //7-5-1944  26-1 Nhâm Thìn
            //13-06-1920 30-04 N
            //28.8.1937 27.10 ÂL
            //1940 14.7 Tân Tỵ
            //24.11 Nhâm Ngọ  28
            //1928 9.11.16 10.10 B Thân
            //1933 Q Dậu 2.5 GNgọ 30.5.2014
            //5.5.1934 8.4 C Tý
            //6.2.1974 27.2.2018 12.1 M Tuất
            //1977
            //1974 2005
            #endregion - Test data

            strSinhTuTuAmLich = strSinhTuTuAmLich.TrimStart('.', ' ', '-').Replace("  ", " ");

            var lstKeyValueSinhTu = new List<KeyValuePair<string, string>>();  //return a list of SinhTu
            string refStrSinh = "";
            string refStrTu = "";
            string refStrTuAl = "";
            List<string> lstSinhTuTuAl = new List<string>();

            string[] arrHyphenSeparator = { " - " };

            string[] arrSinhTuHyphens = strSinhTuTuAmLich.Split(arrHyphenSeparator, StringSplitOptions.RemoveEmptyEntries);


            //Remove ALL the arrSinhTuHyphens[i] == "0" and ONLY add the good one to the lstSinhTuTuAl
            if (arrSinhTuHyphens.Length > 0)
            {
                for (int i = 0; i <= arrSinhTuHyphens.Length - 1; i++)
                {
                    if (arrSinhTuHyphens[i] != "0")
                        lstSinhTuTuAl.Add(arrSinhTuHyphens[i]);
                }
            }

            if (lstSinhTuTuAl.Count > 0)
            {
                //Nov. 15, 1946 - Mar. 23,2009      =>> lstSinhTuTuAl[0] == "Nov. 15, 1946" && lstSinhTuTuAl[1] == "Mar. 23,2009"
                //Oct. 23, 1927 - Apr. 2, 1999      =>> lstSinhTuTuAl[0] == "Oct. 23, 1927" && lstSinhTuTuAl[1] == "Apr. 2, 1999"
                //Sept. 17, 1962 - Nov. 20, 2000    =>> lstSinhTuTuAl[0] == "Sept. 17, 1962" && lstSinhTuTuAl[1] == "Nov. 20, 2000"
                //16-10-1933 - 26-5-2006 ÂL         =>> lstSinhTuTuAl[0] == "16-10-1933" && lstSinhTuTuAl[1] == "26-5-2006 ÂL"
                //7-4-1923 - 22-4-2009 ÂL Kỷ Sửu    =>> lstSinhTuTuAl[0] == "7-4-1923" && lstSinhTuTuAl[1] == "22-4-2009 ÂL Kỷ Sửu"
                //1915 - 2008                       =>> lstSinhTuTuAl[0] == "1915" && lstSinhTuTuAl[1] == "2008
                //0 - 7-4 ÂL                        =>> lstSinhTuTuAl[0] == "0" && lstSinhTuTuAl[1] == "7-4 ÂL"
                //22-10-ÂL                          =>> lstSinhTuTuAl[0] == "22-10-ÂL"


                if (lstSinhTuTuAl.Count == 1)   //Only one date then it must be the "Tu" date
                {
                    //lstSinhTuTuAl[0] == "Nov. 15 1946"
                    if (Util.IsUSMonthFound(lstSinhTuTuAl[0].Trim(), ref refStrTu))
                        lstKeyValueSinhTu.Add(new KeyValuePair<string, string>("Tu", refStrTu));
                    else
                    {
                        //lstSinhTuTuAl[0] == "22-10-ÂL"
                        if (!Util.IsValidDateFmt(lstSinhTuTuAl[0], ref refStrTu))
                        {
                            string[] arrStrTempDate = lstSinhTuTuAl[0].Split(' ');

                            //"22-10-ÂL"
                            //"26-5-2006 ÂL"
                            //"22-4-2009 ÂL Kỷ Sửu"
                            //"1933 2011"
                            //1967 - 1970 & Nguyễn Vô Danh - 1968 - 0
                            //Normally the first index is the date

                            if (arrStrTempDate.Length >= 2)
                            {
                                //1967 - 1970 & Nguyễn Vô Danh - 1968 - 0
                                //[0]1967
                                //[1]
                                string strAmLich = "";
                                if (arrStrTempDate.Length > 2)
                                {
                                    for (int i = 2; i <= arrStrTempDate.Length - 1; i++)
                                        strAmLich += " " + arrStrTempDate[i];     // ÂL Kỷ Sửu
                                }

                                if (Util.IsValidDateFmt(arrStrTempDate[0], ref refStrSinh) && Util.IsValidDateFmt(arrStrTempDate[1], ref refStrTu))
                                {
                                    //"1933 2011"

                                    lstKeyValueSinhTu.Add(new KeyValuePair<string, string>("Sinh", refStrSinh));
                                    lstKeyValueSinhTu.Add(new KeyValuePair<string, string>("Tu", refStrTu));
                                    lstKeyValueSinhTu.Add(new KeyValuePair<string, string>("TuAl", strAmLich.Trim()));
                                }
                                else if (Util.IsValidDateFmt(arrStrTempDate[0], ref refStrTu))
                                {
                                    //arrStrTempDate[0] = "22-4-2009"
                                    //arrStrTempDate[1] = "ÂL"
                                    //arrStrTempDate[2] = "Kỷ"
                                    //arrStrTempDate[3] = "Sửu"

                                    //5.5.1934 8.4 C Tý
                                    //arrStrTempDate[0] = "5.5.1934"
                                    //arrStrTempDate[1] = "8.4"
                                    //arrStrTempDate[2] = "C"
                                    //arrStrTempDate[3] = "Tý"
                                    bool bAmLichDate = false;
                                    strAmLich = "";
                                    for (int i = 1; i <= arrStrTempDate.Length - 1; i++)
                                    {
                                        strAmLich += " " + arrStrTempDate[i];     // ÂL Kỷ Sửu
                                        string[] aTempAmLich = arrStrTempDate[i].Replace('.', '/').Replace('-', '/').Split('/');
                                        if (aTempAmLich.Length >= 2)
                                            bAmLichDate = true;     // 8.4 C Tý
                                    }

                                    if (!bAmLichDate)   //set the TuAl with the new date format >> "4/22/2009 ÂL Kỷ Sửu"
                                        lstKeyValueSinhTu.Add(new KeyValuePair<string, string>("TuAl", refStrTu + strAmLich));
                                    else
                                    {
                                        lstKeyValueSinhTu.Add(new KeyValuePair<string, string>("Tu", refStrTu));      //"5.5.1934"
                                        lstKeyValueSinhTu.Add(new KeyValuePair<string, string>("TuAl", strAmLich.Trim()));   // 8.4 C Tý
                                    }
                                }
                                else
                                    lstKeyValueSinhTu.Add(new KeyValuePair<string, string>("TuAl", lstSinhTuTuAl[0]));  //set the TuAl As Is
                            }
                            else
                            {
                                //"22-10-ÂL"
                                if (Util.IsValidDateFmt(arrStrTempDate[0], ref refStrTu))
                                {
                                    lstKeyValueSinhTu.Add(new KeyValuePair<string, string>("Tu", refStrTu));               //set the Tu = the new date fmt
                                }
                                else
                                    lstKeyValueSinhTu.Add(new KeyValuePair<string, string>("TuAl", lstSinhTuTuAl[0]));  //set the TuAl As Is
                            }
                        }
                        else
                            lstKeyValueSinhTu.Add(new KeyValuePair<string, string>("Tu", refStrTu));
                    }
                }
                else if (lstSinhTuTuAl.Count == 2)
                {
                    //Process lstSinhTuTuAl[0] =>> Sinh
                    //lstSinhTuTuAl[0] == "0" && lstSinhTuTuAl[1] == "7-4 ÂL"
                    if (lstSinhTuTuAl[0] != "0")
                    {
                        //lstSinhTuTuAl[0] == "Nov. 15, 1946" && lstSinhTuTuAl[1] == "Mar. 23,2009"
                        //"7-4-1923" && lstSinhTuTuAl[1] == "22-4-2009 ÂL Kỷ Sửu"
                        if (Util.IsUSMonthFound(lstSinhTuTuAl[0], ref refStrSinh))
                            lstKeyValueSinhTu.Add(new KeyValuePair<string, string>("Sinh", refStrSinh));
                        else
                        {
                            //lstSinhTuTuAl[0] == "16-10-1933" && lstSinhTuTuAl[1] == "26-5-2006 ÂL"
                            //lstSinhTuTuAl[0] == "1933" && lstSinhTuTuAl[1] == "2006"
                            if (!Util.IsValidDateFmt(lstSinhTuTuAl[0], ref refStrSinh))
                            {
                                //lstSinhTuTuAl[0] == "1933" && lstSinhTuTuAl[1] == "2006"
                                lstKeyValueSinhTu.Add(new KeyValuePair<string, string>("Sinh", lstSinhTuTuAl[0]));  //set the Sinh As Is
                            }
                            else
                                lstKeyValueSinhTu.Add(new KeyValuePair<string, string>("Sinh", refStrSinh));
                        }
                    }

                    //Process lstSinhTuTuAl[1] =>> Tu, or TuAl
                    //lstSinhTuTuAl[0] == "0" && lstSinhTuTuAl[1] == "7-4 ÂL"
                    if (lstSinhTuTuAl[1] != "0")
                    {
                        //lstSinhTuTuAl[0] == "Nov. 15, 1946" && lstSinhTuTuAl[1] == "Mar. 23,2009"
                        if (Util.IsUSMonthFound(lstSinhTuTuAl[1], ref refStrTu))
                            lstKeyValueSinhTu.Add(new KeyValuePair<string, string>("Tu", refStrTu));
                        else
                        {
                            //lstSinhTuTuAl[1] == "7-4 ÂL"
                            //lstSinhTuTuAl[1] == "26-5-2006 ÂL"
                            if (!Util.IsValidDateFmt(lstSinhTuTuAl[1], ref refStrTu))
                            {
                                string[] arrStrTempDate = lstSinhTuTuAl[1].Split(' ');

                                //"22-10-ÂL"
                                //"26-5-2006 ÂL"
                                //"22-4-2009 ÂL Kỷ Sửu"
                                //Normally the first index is the date
                                if (Util.IsValidDateFmt(arrStrTempDate[0], ref refStrTuAl))
                                {
                                    //arrStrTempDate[0] = "22-4-2009"
                                    //arrStrTempDate[1] = "ÂL"
                                    //arrStrTempDate[2] = "Kỷ"
                                    //arrStrTempDate[3] = "Sửu"
                                    string strAmLich = "";
                                    for (int i = 1; i <= arrStrTempDate.Length - 1; i++)
                                        strAmLich += " " + arrStrTempDate[i];     // ÂL Kỷ Sửu

                                    //set the TuAl with the new date format >> "4/22/2009 ÂL Kỷ Sửu"
                                    lstKeyValueSinhTu.Add(new KeyValuePair<string, string>("TuAl", refStrTuAl + strAmLich));
                                }
                                else
                                    lstKeyValueSinhTu.Add(new KeyValuePair<string, string>("TuAl", lstSinhTuTuAl[1]));  //set the TuAl As Is
                            }
                            else
                                lstKeyValueSinhTu.Add(new KeyValuePair<string, string>("Tu", refStrTu));
                        }
                    }
                }
                else if (lstSinhTuTuAl.Count > 2)
                {
                    //[0]	"1967"
                    //[1]	"1970 & Nguyễn Vô Danh"
                    //[2]	"1968"

                    //Process lstSinhTuTuAl[0] =>> Sinh
                    //121.10 ĐỖ NGUYÊN DUNG DIANE PD Diệu Hạnh 6.2.1974 27.2.2018 12.1 M Tuất
                    //lstSinhTuTuAl[0] == "Nov. 15, 1946" && lstSinhTuTuAl[1] == "Mar. 23,2009" 
                    //lstSinhTuTuAl[0] == "6.2.1974" && lstSinhTuTuAl[1] == "27.2.2018" && lstSinhTuTuAl[2] == "12.1 M Tuất"
                    if (Util.IsUSMonthFound(lstSinhTuTuAl[0], ref refStrSinh))
                        lstKeyValueSinhTu.Add(new KeyValuePair<string, string>("Sinh", refStrSinh));
                    else
                    {
                        //lstSinhTuTuAl[0] == "16-10-1933" && lstSinhTuTuAl[1] == "26-5-2006 ÂL"
                        //lstSinhTuTuAl[0] == "1933" && lstSinhTuTuAl[1] == "2006"
                        if (!Util.IsValidDateFmt(lstSinhTuTuAl[0], ref refStrSinh))
                        {
                            lstKeyValueSinhTu.Add(new KeyValuePair<string, string>("Sinh", lstSinhTuTuAl[0]));  //set the Sinh As Is; No reformat the date
                        }
                        else
                            lstKeyValueSinhTu.Add(new KeyValuePair<string, string>("Sinh", refStrSinh));
                    }

                    string strTuAmLich = "";
                    for (int i = 2; i <= lstSinhTuTuAl.Count - 1; i++)  //why i=2? because the first[0] normally = Sinh; & [1] = Tu && [2++] is TuAL
                    {
                        strTuAmLich += " " + lstSinhTuTuAl[i];     // 12.1 M Tuất
                    }

                    //Process lstSinhTuTuAl[1] =>> Tu, or TuAl
                    //lstSinhTuTuAl[1] == "27.2.2018"
                    //lstSinhTuTuAl[0] == "Nov. 15, 1946" && lstSinhTuTuAl[1] == "Mar. 23,2009"
                    if (Util.IsUSMonthFound(lstSinhTuTuAl[1], ref refStrTu))
                    {
                        lstKeyValueSinhTu.Add(new KeyValuePair<string, string>("Tu", refStrTu));
                        lstKeyValueSinhTu.Add(new KeyValuePair<string, string>("TuAl", strTuAmLich));
                    }
                    else
                    {
                        //lstSinhTuTuAl[1] == "7-4 ÂL"
                        //lstSinhTuTuAl[1] == "26-5-2006 ÂL"
                        //lstSinhTuTuAl[1] == "27.2.2018"
                        if (!Util.IsValidDateFmt(lstSinhTuTuAl[1], ref refStrTu))
                        {
                            lstKeyValueSinhTu.Add(new KeyValuePair<string, string>("TuAl", lstSinhTuTuAl[1] + " " + strTuAmLich));  //set the TuAl = the rest of the array
                        }
                        else
                        {
                            lstKeyValueSinhTu.Add(new KeyValuePair<string, string>("Tu", refStrTu));
                            lstKeyValueSinhTu.Add(new KeyValuePair<string, string>("TuAl", strTuAmLich));
                        }
                    }
                }
            }
            else
            {
                /*********************************************
                 * looks like it never comes here!!
                 * *******************************************/

                //28.8.1937 27.10 ÂL
                //7-5-1944  26-1 Nhâm Thìn
                //13-06-1920 30-04 N
                //28.8.1937 27.10 ÂL
                //1940 14.7 Tân Tỵ
                //24.11 Nhâm Ngọ  28
                //1928 9.11.16 10.10 B Thân
                //1933 Q Dậu 2.5 GNgọ 30.5.2014
                //5.5.1934 8.4 C Tý
                //6.2.1974 27.2.2018 12.1 M Tuất
                //1977
                //1974 2005

                string[] arrSinhTuSplit = strSinhTuTuAmLich.Split(' ');

                if (arrSinhTuSplit.Length > 0)
                {
                    if (arrSinhTuSplit.Length == 1)   //Only one date then it must be the "Tu" date
                    {
                        //arrSinhTuSplit[0] == "22-10-ÂL"
                        if (arrSinhTuSplit[0] != "0")
                        {
                            //arrSinhTuSplit[0] == "Nov. 15, 1946"
                            if (Util.IsUSMonthFound(arrSinhTuSplit[0], ref refStrTu))
                                lstKeyValueSinhTu.Add(new KeyValuePair<string, string>("Tu", refStrTu));
                            else
                            {
                                //arrSinhTuSplit[0] == "22-10-ÂL"
                                if (!Util.IsValidDateFmt(arrSinhTuSplit[0], ref refStrTu))
                                {
                                    string[] arrStrTempDate = arrSinhTuSplit[0].Split(' ');

                                    //"22-10-ÂL"
                                    //"26-5-2006 ÂL"
                                    //"22-4-2009 ÂL Kỷ Sửu"
                                    //Normally the first index is the date
                                    if (Util.IsValidDateFmt(arrStrTempDate[0], ref refStrTuAl))
                                    {
                                        //arrStrTempDate[0] = "22-4-2009"
                                        //arrStrTempDate[1] = "ÂL"
                                        //arrStrTempDate[2] = "Kỷ"
                                        //arrStrTempDate[3] = "Sửu"
                                        string strAmLich = "";
                                        for (int i = 1; i <= arrStrTempDate.Length - 1; i++)
                                            strAmLich += " " + arrStrTempDate[i];     // ÂL Kỷ Sửu

                                        //set the TuAl with the new date format >> "4/22/2009 ÂL Kỷ Sửu"
                                        lstKeyValueSinhTu.Add(new KeyValuePair<string, string>("TuAl", refStrTuAl + strAmLich));
                                    }
                                    else
                                        lstKeyValueSinhTu.Add(new KeyValuePair<string, string>("TuAl", arrSinhTuSplit[0]));  //set the TuAl As Is
                                }
                                else
                                    lstKeyValueSinhTu.Add(new KeyValuePair<string, string>("Tu", refStrTu));
                            }
                        }
                    }
                    else if (arrSinhTuSplit.Length == 2)
                    {
                        //Process arrSinhTuSplit[0] =>> Sinh
                        //arrSinhTuSplit[0] == "0" && arrSinhTuSplit[1] == "7-4 ÂL"
                        if (arrSinhTuSplit[0] != "0")
                        {
                            //arrSinhTuSplit[0] == "Nov. 15, 1946" && arrSinhTuSplit[1] == "Mar. 23,2009"
                            //"7-4-1923" && arrSinhTuSplit[1] == "22-4-2009 ÂL Kỷ Sửu"
                            if (Util.IsUSMonthFound(arrSinhTuSplit[0], ref refStrSinh))
                                lstKeyValueSinhTu.Add(new KeyValuePair<string, string>("Sinh", refStrSinh));
                            else
                            {
                                //arrSinhTuSplit[0] == "16-10-1933" && arrSinhTuSplit[1] == "26-5-2006 ÂL"
                                //arrSinhTuSplit[0] == "1933" && arrSinhTuSplit[1] == "2006"
                                if (!Util.IsValidDateFmt(arrSinhTuSplit[0], ref refStrSinh))
                                {
                                    //arrSinhTuSplit[0] == "1933" && arrSinhTuSplit[1] == "2006"
                                    lstKeyValueSinhTu.Add(new KeyValuePair<string, string>("Sinh", arrSinhTuSplit[0]));  //set the Sinh As Is
                                }
                                else
                                    lstKeyValueSinhTu.Add(new KeyValuePair<string, string>("Sinh", refStrSinh));
                            }
                        }

                        //Process arrSinhTuSplit[1] =>> Tu, or TuAl
                        //arrSinhTuSplit[0] == "0" && arrSinhTuSplit[1] == "7-4 ÂL"
                        if (arrSinhTuSplit[1] != "0")
                        {
                            //arrSinhTuSplit[0] == "Nov. 15, 1946" && arrSinhTuSplit[1] == "Mar. 23,2009"
                            if (Util.IsUSMonthFound(arrSinhTuSplit[1], ref refStrTu))
                                lstKeyValueSinhTu.Add(new KeyValuePair<string, string>("Tu", refStrTu));
                            else
                            {
                                //arrSinhTuSplit[1] == "7-4 ÂL"
                                //arrSinhTuSplit[1] == "26-5-2006 ÂL"
                                if (!Util.IsValidDateFmt(arrSinhTuSplit[1], ref refStrTu))
                                {
                                    string[] arrStrTempDate = arrSinhTuSplit[0].Split(' ');

                                    //"22-10-ÂL"
                                    //"26-5-2006 ÂL"
                                    //"22-4-2009 ÂL Kỷ Sửu"
                                    //Normally the first index is the date
                                    if (Util.IsValidDateFmt(arrStrTempDate[0], ref refStrTuAl))
                                    {
                                        //arrStrTempDate[0] = "22-4-2009"
                                        //arrStrTempDate[1] = "ÂL"
                                        //arrStrTempDate[2] = "Kỷ"
                                        //arrStrTempDate[3] = "Sửu"
                                        string strAmLich = "";
                                        for (int i = 1; i <= arrStrTempDate.Length - 1; i++)
                                            strAmLich += " " + arrStrTempDate[i];     // ÂL Kỷ Sửu

                                        //set the TuAl with the new date format >> "4/22/2009 ÂL Kỷ Sửu"
                                        lstKeyValueSinhTu.Add(new KeyValuePair<string, string>("TuAl", refStrTuAl + strAmLich));
                                    }
                                    else
                                        lstKeyValueSinhTu.Add(new KeyValuePair<string, string>("TuAl", arrSinhTuSplit[0]));  //set the TuAl As Is
                                }
                                else
                                    lstKeyValueSinhTu.Add(new KeyValuePair<string, string>("Tu", refStrTu));
                            }
                        }
                    }
                    else if (arrSinhTuSplit.Length > 2)
                    {
                        string strTuAmLich = "";
                        for (int i = 1; i <= arrSinhTuSplit.Length - 1; i++)
                            strTuAmLich += " " + arrSinhTuSplit[i];     // 12.1 M Tuất

                        //Process arrSinhTuSplit[0] =>> Sinh
                        //121.10 ĐỖ NGUYÊN DUNG DIANE PD Diệu Hạnh 6.2.1974 27.2.2018 12.1 M Tuất
                        if (arrSinhTuSplit[0] != "0")
                        {
                            //arrSinhTuSplit[0] == "Nov. 15, 1946" && arrSinhTuSplit[1] == "Mar. 23,2009" 
                            //arrSinhTuSplit[0] == "6.2.1974" && arrSinhTuSplit[1] == "27.2.2018" && lstSinhTuTuAl[2] == "12.1 M Tuất"
                            if (Util.IsUSMonthFound(arrSinhTuSplit[0], ref refStrSinh))
                                lstKeyValueSinhTu.Add(new KeyValuePair<string, string>("Sinh", refStrSinh));
                            else
                            {
                                //arrSinhTuSplit[0] == "16-10-1933" && arrSinhTuSplit[1] == "26-5-2006 ÂL"
                                //arrSinhTuSplit[0] == "1933" && arrSinhTuSplit[1] == "2006"
                                if (!Util.IsValidDateFmt(arrSinhTuSplit[0], ref refStrSinh))
                                {
                                    //arrSinhTuSplit[0] == "1933" && arrSinhTuSplit[1] == "2006"
                                    lstKeyValueSinhTu.Add(new KeyValuePair<string, string>("Sinh", arrSinhTuSplit[0]));  //set the Sinh As Is
                                }
                                else
                                    lstKeyValueSinhTu.Add(new KeyValuePair<string, string>("Sinh", refStrSinh));
                            }
                        }

                        //Process arrSinhTuSplit[1] =>> Tu, or TuAl
                        //arrSinhTuSplit[1] == "27.2.2018"
                        if (arrSinhTuSplit[1] != "0")
                        {
                            //arrSinhTuSplit[0] == "Nov. 15, 1946" && arrSinhTuSplit[1] == "Mar. 23,2009"
                            if (Util.IsUSMonthFound(arrSinhTuSplit[1], ref refStrTu))
                                lstKeyValueSinhTu.Add(new KeyValuePair<string, string>("Tu", refStrTu));
                            else
                            {
                                //arrSinhTuSplit[1] == "7-4 ÂL"
                                //arrSinhTuSplit[1] == "26-5-2006 ÂL"
                                //arrSinhTuSplit[1] == "27.2.2018"
                                if (!Util.IsValidDateFmt(arrSinhTuSplit[1], ref refStrTu))
                                {
                                    lstKeyValueSinhTu.Add(new KeyValuePair<string, string>("TuAl", arrSinhTuSplit[1] + " " + strTuAmLich));  //set the TuAl = the rest of the array
                                }
                                else
                                {
                                    lstKeyValueSinhTu.Add(new KeyValuePair<string, string>("Tu", refStrTu));
                                    lstKeyValueSinhTu.Add(new KeyValuePair<string, string>("TuAl", strTuAmLich));
                                }
                            }
                        }
                    }

                }
            }

            return lstKeyValueSinhTu;
        }
        private void txtiFilenameParsing_MouseDown(object sender, MouseEventArgs e)
        {
            if (_iClickCount <= 3)
            {
                //MouseDown     - occurs when the mouse button is pressed
                //MouseClick    - occurs when the mouse button is pressed and released

                _ixSelectLength = txtiFilenameParsing.SelectionStart;

                string strFullFileName = txtiFilenameParsing.Text;
                string strPhapDanhSinhTu = "";

                switch (_iClickCount)
                {
                    case 0: //Filenumber
                        txtiFileNumber.Text = strFullFileName.Substring(0, _ixSelectLength).Trim();
                        txtiFilenameParsing.Text = strFullFileName.Substring(_ixSelectLength).Trim();
                        if (txtiFilenameParsing.Text == "")
                            txtiHoTen.Text = "HOA SEN";  //54.3.jpg (Sen) must set txtHTen.Text = "HOA SEN"
                        break;
                    case 1: //HoTen
                        txtiHoTen.Text = strFullFileName.Substring(0, _ixSelectLength).Trim();
                        txtiFilenameParsing.Text = strFullFileName.Substring(_ixSelectLength).Trim();
                        break;
                    case 2: //Phap Danh or Sinh or Tu
                    case 3:
                        strPhapDanhSinhTu = strFullFileName.Substring(0, _ixSelectLength).Trim();
                        if (strPhapDanhSinhTu.ToUpper().IndexOf("PD ") == 0)
                        {
                            txtiPhapDanh.Text = strPhapDanhSinhTu.Substring(3);
                        }
                        else
                        {
                            List<KeyValuePair<string, string>> lstKeyValues = SinhTuTuAlFmt(strPhapDanhSinhTu);

                            foreach (KeyValuePair<string, string> kValue in lstKeyValues)
                            {
                                //Sinh, Tu, TuAl
                                if (kValue.Key == "Sinh")
                                    txtiSinh.Text = kValue.Value;
                                else if (kValue.Key == "Tu")
                                    txtiTu.Text = kValue.Value;
                                else if (kValue.Key == "TuAl")
                                    txtiTuAL.Text = kValue.Value;
                            }
                        }
                        txtiFilenameParsing.Text = strFullFileName.Substring(_ixSelectLength).Trim();
                        break;
                }

                _iClickCount++;
            }
        }
        private void Insert_HL()
        {
            string strOrigImgFileName = lbliOrigFilename.Text.Trim();
            string strNewImgFileName = txtiFilename.Text.Trim();
            try
            {
                //Insert New HL into DB
                tblHLTableAdapter.Insert(txtiHoTen.Text, txtiPhapDanh.Text, txtiSinh.Text.Trim(',').TrimEnd('-').TrimEnd('/').TrimEnd('.'), txtiTu.Text.Trim(',').TrimEnd('-').TrimEnd('/').TrimEnd('.'), txtiViTriHinh.Text, txtiViTriCot.Text.Trim(), txtiFilename.Text, txtiNote.Text.Trim(), txtiFileNumber.Text.Trim(',').TrimEnd('-').TrimEnd('/').TrimEnd('.'), lbliFullname.Text.Trim(), txtiTuAL.Text, DateTime.Now.ToString("G"), null, lbliFullPhapDanh.Text.Trim(), null);

                tblHLBindingSource.EndEdit();
                tblHLTableAdapter.Update(this.dN_HLDataSet.tblHL);  //Update the HL_DB table

                //release Image filename from HL ListBox in order for the File.Move() to work!
                lstiBoxHLImg.Items.RemoveAt(lstiBoxHLImg.SelectedIndex);

                //release Picturebox's image in order for the Util.MoveFile()-File.Move() to work!
                ClearInsertPicBox();

                //Rename the imagefilename in imgFolderPath and then MOVE it to imgFolderDonePath after save HL data and image filename in HL_DB
                if (!strOrigImgFileName.Equals(strNewImgFileName))
                    Util.FileSaveAsAndMove(_ImgFolderPath, _ImgFolderDonePath, strOrigImgFileName, strNewImgFileName);
                else
                    Util.MoveFile(_ImgFolderPath, _ImgFolderDonePath, strNewImgFileName);

                Util.LogAMessage(_logFile, $"Inserted '{_ImgFolderDonePath}{strOrigImgFileName}'");
            }
            catch (Exception e)
            {
                Util.LogAMessage(_errorFile, $"Failed to add '{_ImgFolderDonePath}{strOrigImgFileName}'. Exception: '{e.Message}'");
                throw new Exception(e.Message);
            }
        }
        private void btniSave_Click(object sender, EventArgs e)
        {
            if (txtiHoTen.Text.Trim() != "")
            {
                try
                {
                    string strEnglishHoTen = Convert_VN_To_Eng(txtiHoTen.Text.Trim());        //must remove Vietnamese accent from HoTen
                    string strEnglishPhapDanh = Convert_VN_To_Eng(txtiPhapDanh.Text.Trim());    //must remove Vietnamese accent from PhapDanh

                    if (strEnglishHoTen.Length > 0)
                        lbliFullname.Text = strEnglishHoTen;

                    if (strEnglishPhapDanh.Length > 0)
                        lbliFullPhapDanh.Text = strEnglishPhapDanh;

                    string strDupHLImgage = GetDupHL(txtiFilename.Text, lbliFullname.Text);

                    if (strDupHLImgage == "")
                    {
                        Insert_HL();

                        if (lstiBoxHLImg.Items.Count >= 1)
                            lstiBoxHLImg.SelectedIndex = 0;     //refresh HL image and move to the next HL data
                        else
                            lstiBoxHLImg.SelectedIndex = -1;
                    }
                    else
                    {
                        DisplayDupImage(strDupHLImgage);

                        if (MessageBox.Show("Possible duplicate Huong Linh found.\r\n\r\n\r\n" + txtiFilename.Text + "\r\n\r\nInsert?", "Message", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                        {
                            //YES! INSERT THIS PICTURE AND MOVE ON TO THE NEXT HL IMAGE

                            ClearDupPicBox();   //clear the Duplicate image pictureBox

                            Insert_HL();

                            if (lstiBoxHLImg.Items.Count >= 1)
                                lstiBoxHLImg.SelectedIndex = 0;     //refresh HL image and move to the next HL data
                            else
                                lstiBoxHLImg.SelectedIndex = -1;
                        }
                        else
                        {
                            if (MessageBox.Show(txtiFilename.Text + "\r\nMove this HL to Archive Folder?", "Message", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                            {
                                //DO NOT INSERT THIS PICTURE! MOVE THIS HL IMAGE TO THE ARCHIVE FOLDER

                                //release Image filename from HL ListBox in order for the File.Move() to work!
                                lstiBoxHLImg.Items.RemoveAt(lstiBoxHLImg.SelectedIndex);

                                //release Picturebox's image in order for the File.Move() to work!
                                ClearInsertPicBox();

                                //release Picturebox's Duplicate image in order for the File.Move() to work!
                                ClearDupPicBox();

                                //if (strOrigImgFileName != strNewImgFileName)
                                //    Util.FileSaveAsAndMove(_ImgFolderPath, _ImgFolderArchivePath, strOrigImgFileName, strNewImgFileName);
                                //else
                                Util.MoveFile(_ImgFolderPath, _ImgFolderArchivePath, txtiFilename.Text.Trim());

                                if (lstiBoxHLImg.Items.Count >= 1)
                                    lstiBoxHLImg.SelectedIndex = 0;     //refresh HL image and move to the next HL data
                                else
                                    lstiBoxHLImg.SelectedIndex = -1;
                            }
                        }
                    }
                }
                catch (System.Data.OleDb.OleDbException ex) //(Exception ex)
                {
                    MessageBox.Show(ex.Message, "Message", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    tblHLBindingSource.CancelEdit();            //roll back
                    tblHLBindingSource.ResetBindings(false);
                }
            }
            else
            {
                MessageBox.Show("Please enter Filenumber, HoTen, ... before click SAVE!", "Message", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion - Insert Tab Section

        #region =========================== functions NOT USE in this project



        /// <summary>
        /// Do Not use this function for CDN
        /// </summary>
        /// <returns></returns>
        //private string ReFormatImageFileNameForInsertFilename()
        //{
        //    string strNewFileName = string.Format("{0}.jpg", txtsFileNumber.Text);   //54.2.jpg

        //    if (txtsHoTen.Text.Trim().Length > 0 && txtsPhapDanh.Text.Trim().Length > 0 && txtsSinh.Text.Trim().Length > 0 && txtsTu.Text.Trim().Length > 0)
        //        strNewFileName = string.Format("{0} {1} Pd {2} {3} {4}.jpg", txtsFileNumber.Text, txtsHoTen.Text.Trim(), txtsPhapDanh.Text.Trim(), txtsSinh.Text.Trim().Replace("/",".").Replace("-", "."), txtsTu.Text.Trim().Replace("/", ".").Replace("-", "."));
        //    else if (txtsHoTen.Text.Trim().Length > 0 && txtsPhapDanh.Text.Trim().Length > 0 && txtsSinh.Text.Trim().Length > 0)
        //        strNewFileName = string.Format("{0} {1} Pd {2} {3}.jpg", txtsFileNumber.Text, txtsHoTen.Text.Trim(), txtsPhapDanh.Text.Trim(), txtsSinh.Text.Trim().Replace("/", ".").Replace("-", "."));
        //    else if (txtsHoTen.Text.Trim().Length > 0 && txtsPhapDanh.Text.Trim().Length > 0 && txtsTu.Text.Trim().Length > 0)
        //        strNewFileName = string.Format("{0} {1} Pd {2} {3}.jpg", txtsFileNumber.Text, txtsHoTen.Text.Trim(), txtsPhapDanh.Text.Trim(), txtsTu.Text.Trim().Replace("/", ".").Replace("-", "."));
        //    else if (txtsHoTen.Text.Trim().Length > 0 && txtsPhapDanh.Text.Trim().Length > 0)
        //        strNewFileName = string.Format("{0} {1} Pd {2}.jpg", txtsFileNumber.Text, txtsHoTen.Text.Trim(), txtsPhapDanh.Text.Trim());
        //    else if (txtsHoTen.Text.Trim().Length > 0 && txtsSinh.Text.Trim().Length > 0)
        //        strNewFileName = string.Format("{0} {1} {2}.jpg", txtsFileNumber.Text, txtsHoTen.Text.Trim(), txtsSinh.Text.Trim().Replace("/", ".").Replace("-", "."));
        //    else if (txtsHoTen.Text.Trim().Length > 0 && txtsTu.Text.Trim().Length > 0)
        //        strNewFileName = string.Format("{0} {1} {2}.jpg", txtsFileNumber.Text, txtsHoTen.Text.Trim(), txtsTu.Text.Trim().Replace("/", ".").Replace("-", "."));
        //    else if (txtsHoTen.Text.Trim().Length > 0)
        //        strNewFileName = string.Format("{0} {1}.jpg", txtsFileNumber.Text, txtsHoTen.Text.Trim());

        //    return strNewFileName;
        //}

        //private void Display_HLName_Image()
        //{
        //    //Not use

        //    //dataGridView.ClearSelection();
        //    //dataGridView.CurrentCell = dataGridView.Rows[0].Cells[0];
        //    //dataGridView.CurrentCell.Selected = true;

        //    txtOrigImgFileName.Text = lstBoxHLImg.SelectedItem.ToString();
        //    txtImgFilename.Text = lstBoxHLImg.SelectedItem.ToString().Replace(".jpg", "");

        //    DisplayImageInsert();

        //}


        /// <summary>
        /// When HL Name or PhapDanh changed, must change ImageFileName as well to reflex the HL Name and or PhapDanh in the image filename
        /// ex:(from: 1 Ngo Dung Pd Nhat Doan 1919-2002.jpg  >>>  1 Ngô Dung Pd Nhật Đoan 1919-2002.jpg 
        /// </summary>
        /// <returns></returns>
        //private string ReFormatImageFileName()
        //{

        //    /// //Replace the New filenumber in the txtsFilename.Text
        //    ///if (txtsFileNumber.Text.Trim() != lblsOrigFileNumber.Text)
        //    ///    txtsFilename.Text = txtsFilename.Text.Replace(lblsOrigFileNumber.Text, txtsFileNumber.Text.Trim());
        //    ///
        //    /// //Replace the New HoTen in the txtsFilename.Text
        //    ///if (txtsHoTen.Text.Trim() != lblsOrigFilename.Text)
        //    ///    txtsFilename.Text = txtsFilename.Text.Replace(lblsOrigFilename.Text, txtsHoTen.Text.Trim());
        //    ///
        //    /// //Replace the New Phap Danh in the txtsFilename.Text
        //    ///if (txtsPhapDanh.Text.Trim() != lblsOrigPhapDanh.Text)
        //    ///{
        //    ///    if (lblsOrigPhapDanh.Text == "")
        //    ///    {
        //    ///        txtsFilename.Text = txtsFilename.Text.Replace(lblsOrigPhapDanh.Text, " PD " + txtsPhapDanh.Text.Trim());        //Add New Phap Danh
        //    ///        strNewFileName = txtsFileNumber.Text.Trim() + " " + txtsHoTen.Text.Trim() + " PD " + txtsPhapDanh.Text.Trim();
        //    ///    }
        //    ///    else
        //    ///    {
        //    ///        txtsFilename.Text = txtsFilename.Text.Replace(lblsOrigPhapDanh.Text, txtsPhapDanh.Text.Trim());                 //Update Phap Danh
        //    ///        strNewFileName = txtsFileNumber.Text.Trim() + " " + txtsHoTen.Text.Trim() + " PD " + txtsPhapDanh.Text.Trim();
        //    ///    }
        //    ///}

        //    string strNewFileName = "";
        //    string strNewSinhTu = "";

        //    if (txtsTuAL.Text.Trim() != lblsOrigTuAl.Text)
        //    {
        //        //txtsTuAL.Text !=  lblsOrigTuAl.Text   =>  strSinhTu
        //        //-------------     -----------------       ---------
        //        //  ABC         !=        123           =>     ABC
        //        //  ABC         !=        ""            =>     ABC
        //        //  ""          !=        123           =>     ""

        //        if (txtsTuAL.Text != "")
        //            strNewSinhTu = txtsTuAL.Text.Trim().Replace('/', '.');
        //    }
        //    else if (txtsTuAL.Text.Trim() != "")
        //    {
        //        //txtsTuAL.Text ==  lblsOrigTuAl.Text   =>  strSinhTu
        //        //-------------     -----------------       ---------
        //        //  ABC         ==        ABC           =>     ABC
        //        //  ""          ==        ""            =>     ""

        //        strNewSinhTu = txtsTuAL.Text.Trim().Replace('/', '.');
        //    }

        //    if (txtsTu.Text.Trim() != lblsOrigTu.Text)
        //    {
        //        //txtsTu.Text !=  lblsOrigTu.Text   =>  strSinhTu
        //        //-----------     -----------------     ---------
        //        //  ABC       !=        123         =>     ABC
        //        //  ABC       !=        ""          =>     ABC
        //        //  ""        !=        123         =>     ""

        //        if (txtsTu.Text != "")
        //        {
        //            if (strNewSinhTu.Trim() == "")
        //                strNewSinhTu = txtsTu.Text.Trim().Replace('/', '.');
        //            else
        //                strNewSinhTu = txtsTu.Text.Trim().Replace('/', '.') + " " + strNewSinhTu;
        //        }
        //    }
        //    else if (txtsTu.Text.Trim() != "")
        //    {
        //        //txtsTu.Text ==  lblsOrigTu.Text   =>  strSinhTu
        //        //-----------     -----------------     ---------
        //        //  ABC       ==        ABC         =>     ABC
        //        //  ""        ==        ""          =>     ""

        //        if (strNewSinhTu.Trim() == "")
        //            strNewSinhTu = txtsTu.Text.Trim().Replace('/', '.');
        //        else
        //            strNewSinhTu = txtsTu.Text.Trim().Replace('/', '.') + " " + strNewSinhTu;
        //    }

        //    if (txtsSinh.Text.Trim() != lblsOrigSinh.Text)
        //    {
        //        //txtsSinh.Text !=  lblsOrigSinh.Text   =>  strSinhTu
        //        //-------------     -----------------       ---------
        //        //  ABC         !=        123           =>     ABC
        //        //  ABC         !=        ""            =>     ABC
        //        //  ""          !=        123           =>     ""

        //        if (txtsSinh.Text != "")
        //        {
        //            if (strNewSinhTu.Trim() == "")
        //                strNewSinhTu = txtsSinh.Text.Trim().Replace('/', '.');
        //            else
        //                strNewSinhTu = txtsSinh.Text.Trim().Replace('/', '.') + " " + strNewSinhTu;
        //        }
        //    }
        //    else if (txtsSinh.Text.Trim() != "")
        //    {
        //        //txtsSinh.Text !=  lblsOrigSinh.Text   =>  strSinhTu
        //        //-------------     -----------------       ---------
        //        //  ABC         ==        ABC           =>     ABC
        //        //  ""          ==        ""            =>     ""

        //        if (strNewSinhTu.Trim() == "")
        //            strNewSinhTu = txtsSinh.Text.Trim().Replace('/', '.');
        //        else
        //            strNewSinhTu = txtsSinh.Text.Trim().Replace('/', '.') + " " + strNewSinhTu;
        //    }

        //    if (txtsPhapDanh.Text.Trim() != "")
        //        strNewFileName = txtsFileNumber.Text.Trim() + " " + txtsHoTen.Text.Trim() + " PD " + txtsPhapDanh.Text.Trim() + " " + strNewSinhTu;
        //    else
        //        strNewFileName = txtsFileNumber.Text.Trim() + " " + txtsHoTen.Text.Trim() + " " + strNewSinhTu;

        //    return strNewFileName.Trim() + ".jpg";
        //}

        /// <summary>
        /// DO NOT USE THIS FUNCTION FOR CDN
        /// When the txtHTen.Text = ""; must set the txtHTen.Text = "HOA SEN"
        /// txtFNumb.Text + txtHTen.Text + txtPDanh.Text + txtSiTu.Text. 
        /// When the HL Name or PhapDanh changed, must change the ImageFileName as well to reflex the HL Name and or PhapDanh 
        /// ex:(from: 1 Ngo Dung Pd Nhat Doan 1919-2002.jpg  >>>  1 Ngô Dung Pd Nhật Đoan 1919-2002.jpg 
        /// </summary>
        /// <returns></returns>
        //private string ReFormatInsertImageFileName()
        //{
        //    string strNewImgFileName = string.Format("{0}.jpg", txtiFileNumber.Text);   //54.2.jpg

        //    if (txtiHoTen.Text.Trim().Length > 0 && txtiPhapDanh.Text.Trim().Length > 0 && txtiSinh.Text.Trim().Length > 0 && txtiTu.Text.Trim().Length > 0)
        //        strNewImgFileName = string.Format("{0} {1} Pd {2} {3}.jpg", txtiFileNumber.Text, txtiHoTen.Text.Trim(), txtiPhapDanh.Text.Trim(), txtiSinh.Text.Trim(), txtiTu.Text.Trim());
        //    else if (txtiHoTen.Text.Trim().Length > 0 && txtiPhapDanh.Text.Trim().Length > 0 && txtiSinh.Text.Trim().Length > 0)
        //        strNewImgFileName = string.Format("{0} {1} Pd {2} {3}.jpg", txtiFileNumber.Text, txtiHoTen.Text.Trim(), txtiPhapDanh.Text.Trim(), txtiSinh.Text.Trim());
        //    else if (txtiHoTen.Text.Trim().Length > 0 && txtiPhapDanh.Text.Trim().Length > 0 && txtiTu.Text.Trim().Length > 0)
        //        strNewImgFileName = string.Format("{0} {1} Pd {2} {3}.jpg", txtiFileNumber.Text, txtiHoTen.Text.Trim(), txtiPhapDanh.Text.Trim(), txtiTu.Text.Trim());
        //    else if (txtiHoTen.Text.Trim().Length > 0 && txtiPhapDanh.Text.Trim().Length > 0)
        //        strNewImgFileName = string.Format("{0} {1} Pd {2}.jpg", txtiFileNumber.Text, txtiHoTen.Text.Trim(), txtiPhapDanh.Text.Trim());
        //    else if (txtiHoTen.Text.Trim().Length > 0 && txtiSinh.Text.Trim().Length > 0)
        //        strNewImgFileName = string.Format("{0} {1} {2}.jpg", txtiFileNumber.Text, txtiHoTen.Text.Trim(), txtiSinh.Text.Trim());
        //    else if (txtiHoTen.Text.Trim().Length > 0 && txtiTu.Text.Trim().Length > 0)
        //        strNewImgFileName = string.Format("{0} {1} {2}.jpg", txtiFileNumber.Text, txtiHoTen.Text.Trim(), txtiTu.Text.Trim());
        //    else if (txtiHoTen.Text.Trim().Length > 0)
        //        strNewImgFileName = string.Format("{0} {1}.jpg", txtiFileNumber.Text, txtiHoTen.Text.Trim());
        //    else if (txtiHoTen.Text.Trim().Length <= 0)
        //    {
        //        txtiHoTen.Text = "HOA SEN";
        //        strNewImgFileName = string.Format("{0} {1}.jpg", txtiFileNumber.Text, txtiHoTen.Text.Trim());
        //    }
        //    return strNewImgFileName;
        //}

        //private bool SearchStringLike(string[] astrSearch)
        //{
        //    bool bDataFound = false;

        //    for (int i = astrSearch.Length - 1; i >= 0; --i)
        //    {
        //        if (!bDataFound)
        //        {
        //            string strNewSearch = "";
        //            string strRowFilter = "";
        //            string strSearch = " " + astrSearch[i];

        //            for (int x = 0; x < i; x++)
        //                strNewSearch += " " + astrSearch[x];                            //" Diep The"

        //            //strRowFilter = "(Fullname like '" + strNewSearch.TrimStart() + strSearch;      //"(Fullname like 'Diep The Hung%')"
        //            strRowFilter = "Fullname like '" + strNewSearch.TrimStart() + "%' OR Fullname like '" + strSearch + "%'";      //"(Fullname like 'Diep The Hung%')"

        //            if (strRowFilter.Length > 0 && !bDataFound)
        //            {
        //                dN_HLDataSet.tblHL.CaseSensitive = false;  //search (upper/lower) cases
        //                dN_HLDataSet.tblHL.DefaultView.RowFilter = strRowFilter;
        //                dN_HLDataSet.tblHL.DefaultView.Sort = "HoTen ASC";

        //                if (dN_HLDataSet.tblHL.DefaultView.Count > 10)
        //                {
        //                    strRowFilter = "Fullname like '" + strNewSearch.TrimStart() + "%'";

        //                    dN_HLDataSet.tblHL.DefaultView.RowFilter = strRowFilter;               //filter 1st time

        //                    dN_HLDataSet.tblHL.AcceptChanges();

        //                    //1. copy the 1st filter result to tbSearchResult1
        //                    //2. then use this table to filter the sencond time
        //                    DataTable tbSearchResult1 = dN_HLDataSet.tblHL.DefaultView.ToTable("tblResult1", false, "ID", "HoTen", "PhapDanh", "SinhNgay_DL", "MatNgay_DL", "ViTriHinh", "ViTriCot", "HinhFileNamePath", "Note", "Fullname", "FileNumber", "MatNgay_AL", "InsertDate", "UpdateDate", "FullPhapDanh");

        //                    if (tbSearchResult1.Rows.Count > 0)
        //                    {
        //                        string strTemp = tbSearchResult1.Rows[0]["Fullname"].ToString();

        //                        strRowFilter = "Fullname like '%" + strSearch + "%'";

        //                        DataView dvSearchResult2 = tbSearchResult1.DefaultView;
        //                        dvSearchResult2.RowFilter = strRowFilter;                                //filter 2nd time

        //                        if (dvSearchResult2.Table.Rows.Count > 0)
        //                        {
        //                            dvSearchResult2.Sort = "Fullname ASC";
        //                            tblHLBindingSource.DataSource = dvSearchResult2;
        //                            datasGridView.DataSource = tblHLBindingSource;
        //                            bDataFound = true;
        //                            break;
        //                        }
        //                    }
        //                    ////////"txtsSearch" == (Diep The Hung%) and data found in ["Fullname"], Bind Data Source and return results
        //                    //////tblHLBindingSource.DataSource = dN_HLDataSet.tblHL;
        //                    //////dataGridView.DataSource = tblHLBindingSource;
        //                    //////bDataFound = true;
        //                    //////break;
        //                }
        //                else if (dN_HLDataSet.tblHL.DefaultView.Count > 0)
        //                {
        //                    tblHLBindingSource.DataSource = dN_HLDataSet.tblHL;
        //                    datasGridView.DataSource = tblHLBindingSource;
        //                    bDataFound = true;
        //                    break;
        //                }
        //                else
        //                {
        //                    strSearch = strSearch.Trim().Replace("%')", "");              //"Hunz"
        //                    char[] aSearch = strSearch.ToCharArray();                     //('H','u','n','z')

        //                    for (int y = aSearch.Length - 1; y > 0; --y)
        //                    {
        //                        if (!bDataFound)
        //                        {
        //                            strSearch = "";
        //                            for (int z = 0; z < y; z++)
        //                                strSearch += aSearch[z];                            //"Hun"

        //                            strRowFilter = "(Fullname like '" + strNewSearch.TrimStart() + " " + strSearch + "%')";      //"(Fullname like 'Diep The Hun%')"

        //                            if (strRowFilter.Length > 0 && !bDataFound)
        //                            {
        //                                dN_HLDataSet.tblHL.DefaultView.RowFilter = strRowFilter;
        //                                if (dN_HLDataSet.tblHL.DefaultView.Count > 0)
        //                                {
        //                                    //"txtsSearch" == (Diep The Hun%) and data found in ["Fullname"], Bind Data Source and return results
        //                                    dN_HLDataSet.tblHL.DefaultView.Sort = "Fullname ASC";
        //                                    dN_HLDataSet.tblHL.AcceptChanges();
        //                                    tblHLBindingSource.DataSource = dN_HLDataSet.tblHL;
        //                                    datasGridView.DataSource = tblHLBindingSource;
        //                                    bDataFound = true;
        //                                    break;
        //                                }
        //                            }
        //                        }
        //                        else
        //                            break;
        //                    }
        //                }
        //            }

        //        }
        //        else
        //            break;
        //    }

        //    return bDataFound;
        //}

        //private bool SearchStringAsIs(string[] astrSearch)
        //{
        //    bool bDataFound = false;

        //    for (int i = astrSearch.Length - 1; i >= 0; --i)
        //    {
        //        if (!bDataFound)
        //        {
        //            string strNewSearch = "";
        //            string strRowFilter = "";
        //            string strSearch = " " + astrSearch[i] + "%')";      //"Hung%')"

        //            for (int x = 0; x < i; x++)
        //                strNewSearch += " " + astrSearch[x];                            //" Diep The"

        //            strRowFilter = "(Fullname like '" + strNewSearch.TrimStart() + strSearch;      //"(Fullname like 'Diep The Hung%')"

        //            if (strRowFilter.Length > 0 && !bDataFound)
        //            {
        //                dN_HLDataSet.tblHL.CaseSensitive = false;  //search (upper/lower) cases
        //                dN_HLDataSet.tblHL.DefaultView.RowFilter = strRowFilter;
        //                dN_HLDataSet.tblHL.DefaultView.Sort = "HoTen ASC";
        //                if (dN_HLDataSet.tblHL.DefaultView.Count > 0)
        //                {
        //                    //"txtsSearch" == (Diep The Hung%) and data found in ["Fullname"], Bind Data Source and return results
        //                    tblHLBindingSource.DataSource = dN_HLDataSet.tblHL;
        //                    datasGridView.DataSource = tblHLBindingSource;
        //                    bDataFound = true;
        //                    break;
        //                }
        //                else
        //                {
        //                    strSearch = strSearch.Trim().Replace("%')", "");              //"Hunz"
        //                    char[] aSearch = strSearch.Trim().ToCharArray();                     //('H','u','n','z')

        //                    for (int y = aSearch.Length - 1; y > 0; --y)
        //                    {
        //                        if (!bDataFound)
        //                        {
        //                            strSearch = "";
        //                            for (int z = 0; z < y; z++)
        //                                strSearch += aSearch[z];                            //"Hun"

        //                            strRowFilter = "(Fullname like '" + strNewSearch.TrimStart() + " " + strSearch + "%')";      //"(Fullname like 'Diep The Hun%')"

        //                            if (strRowFilter.Length > 0 && !bDataFound)
        //                            {
        //                                dN_HLDataSet.tblHL.DefaultView.RowFilter = strRowFilter;
        //                                dN_HLDataSet.tblHL.DefaultView.Sort = "HoTen ASC";
        //                                if (dN_HLDataSet.tblHL.DefaultView.Count > 0)
        //                                {
        //                                    //"txtsSearch" == (Diep The Hun%) and data found in ["Fullname"], Bind Data Source and return results
        //                                    tblHLBindingSource.DataSource = dN_HLDataSet.tblHL;
        //                                    datasGridView.DataSource = tblHLBindingSource;

        //                                    bDataFound = true;
        //                                    break;
        //                                }
        //                            }
        //                        }
        //                        else
        //                            break;
        //                    }
        //                }
        //            }

        //        }
        //        else
        //            break;
        //    }

        //    return bDataFound;
        //}

        //private bool SearchNameArrayOriginal(string[] astrSearchName)
        //{
        //    bool bDataFound = false;
        //    int ix = astrSearchName.Length - 1;

        //    if (!bDataFound)
        //    {
        //        string strFirstIxName = "";
        //        string strRowFilter = "";
        //        string strLastIxName = astrSearchName[ix];
        //        string strSearch = " " + astrSearchName[ix];                    //Fullname:    nguyen hoang phuong
        //                                                                        //search name: nguyen phuong
        //        for (int iy = 0; iy < ix; iy++)                                 //astrSearchName:   [0]nguyen
        //            strFirstIxName += " " + astrSearchName[iy];                 //                  [1]phuong

        //        strRowFilter = "Fullname like '%" + strLastIxName + "%'";       //set filter = strLastIxSearch;  Fullname like '%phuong%'

        //        if (strRowFilter.Length > 0 && !bDataFound)
        //        {
        //            dN_HLDataSet.tblHL.CaseSensitive = false;                   //search (upper/lower) cases
        //            dN_HLDataSet.tblHL.DefaultView.RowFilter = strRowFilter;    //filter: 1st time by LastIxSearch

        //            if (dN_HLDataSet.tblHL.DefaultView.Count > 0)
        //            {
        //                dN_HLDataSet.tblHL.AcceptChanges();

        //                //1. copy the 1st filter result to tbSearchResult1
        //                //2. then use this table to filter the sencond time
        //                DataTable tbSearchResult1 = dN_HLDataSet.tblHL.DefaultView.ToTable("tblResult1", false, "ID", "HoTen", "PhapDanh", "SinhNgay_DL", "MatNgay_DL", "ViTriHinh", "ViTriCot", "HinhFileNamePath", "Note", "Fullname", "FileNumber", "MatNgay_AL", "InsertDate", "UpdateDate", "FullPhapDanh");

        //                if (tbSearchResult1.Rows.Count > 0)
        //                {
        //                    string strTemp = tbSearchResult1.Rows[0]["Fullname"].ToString();

        //                    strRowFilter = "Fullname like '" + strFirstIxName.Trim() + "%'";     //set filter = strFirstIxSearch

        //                    DataView dvSearchResult2 = tbSearchResult1.DefaultView;
        //                    dvSearchResult2.RowFilter = strRowFilter;                                //filter 2nd time from tbSearchResult1

        //                    if (dvSearchResult2.Count > 0)
        //                        bDataFound = true;

        //                    dvSearchResult2.Sort = "Fullname ASC";
        //                    tblHLBindingSource.DataSource = dvSearchResult2;
        //                    datasGridView.DataSource = tblHLBindingSource;
        //                    datasGridView.Focus();

        //                }
        //            }
        //            else
        //            {
        //                strSearch = strSearch.Trim().Replace("%')", "");              //"Hunz"
        //                char[] aSearch = strSearch.ToCharArray();                     //('H','u','n','z')

        //                for (int y = aSearch.Length - 1; y > 0; --y)
        //                {
        //                    if (!bDataFound)
        //                    {
        //                        strSearch = "";
        //                        for (int z = 0; z < y; z++)
        //                            strSearch += aSearch[z];                            //"Hun"

        //                        strRowFilter = "(Fullname like '" + strFirstIxName.Trim() + " " + strSearch + "%')";      //"(Fullname like 'Diep The Hun%')"

        //                        if (strRowFilter.Length > 0 && !bDataFound)
        //                        {
        //                            dN_HLDataSet.tblHL.DefaultView.RowFilter = strRowFilter;
        //                            dN_HLDataSet.tblHL.DefaultView.Sort = "HoTen ASC";
        //                            if (dN_HLDataSet.tblHL.DefaultView.Count > 0)
        //                            {
        //                                //"txtsSearch" == (Diep The Hun%) and data found in ["Fullname"], Bind Data Source and return results
        //                                tblHLBindingSource.DataSource = dN_HLDataSet.tblHL;
        //                                datasGridView.DataSource = tblHLBindingSource;
        //                                datasGridView.Focus();
        //                                bDataFound = true;
        //                                break;
        //                            }
        //                        }
        //                    }
        //                    else
        //                        break;
        //                }
        //            }
        //        }

        //    }
        //    return bDataFound;
        //}

        #endregion =========================== functions NOT USE in this project

        #region ===================== new methods
        private void btnLoadLogFile_Click(object sender, EventArgs e)
        {
            LoadLogFiles();
        }

        private void LoadLogFiles()
        {
            using (StreamReader reader = new StreamReader(_logFile))
                txtLogFileText.Text = reader.ReadToEnd();
            using (StreamReader reader = new StreamReader(_errorFile))
                txtErrorFileText.Text = reader.ReadToEnd();
        }

        private void btnClearLogFile_Click(object sender, EventArgs e)
        {
            File.Delete(_logFile);
            using (FileStream fs = new FileStream(_logFile, FileMode.Append, FileAccess.Write))
            {
                StreamWriter sw = new StreamWriter(fs);
                sw.WriteLine(DateTime.Now + " Clear all!");
                sw.Flush();
                fs.Close();
            }

            File.Delete(_errorFile);
            using (FileStream fs = new FileStream(_errorFile, FileMode.Append, FileAccess.Write))
            {
                StreamWriter sw = new StreamWriter(fs);
                sw.WriteLine(DateTime.Now + " Clear all!");
                sw.Flush();
                fs.Close();
            }

            LoadLogFiles();
        }

        private void btnSelectInsertFile_Click(object sender, EventArgs e)
        {
            var fileList = new string[0];
            using (var openFileDialog = new OpenFileDialog())
            {
                openFileDialog.InitialDirectory = _ImgFolderPath;
                openFileDialog.Filter = "Images (*.JPG;*.JPEG;*.PNG)|*.JPG;*.JPEG;*.PNG";
                openFileDialog.RestoreDirectory = true;
                openFileDialog.Multiselect = true;

                if (openFileDialog.ShowDialog() != DialogResult.OK)
                    return; //No file to load
                fileList = openFileDialog.FileNames;
            }

            // Resize all selected images
            foreach (string strImgFileName in fileList)
            {
                var destImgFileName = $"{_ImgFolderPath}{Path.GetFileName(strImgFileName)}";
                try
                {
                    if (Util.ResizeImageAndSave(strImgFileName, destImgFileName) == false)
                    {
                        MessageBox.Show($"Unable to resize and save an image '{strImgFileName}'. See ErrLog.txt for details.");
                        Util.LogAMessage(_errorFile, $"Failed to resize and save image from '{strImgFileName}' to '{destImgFileName}'. " +
                            Environment.NewLine + $"'{strImgFileName}' may have already existed.");
                        continue;
                    }
                }
                catch(Exception ex)
                {
                    Util.LogAMessage(_errorFile, $"Failed to resize and save image from '{strImgFileName}' to '{destImgFileName}'. " +
                        Environment.NewLine + $"'{strImgFileName}' may have already existed." +
                        Environment.NewLine + $"Exception: '{ex.Message}'.");
                    continue;
                }
                Util.LogAMessage(_logFile, $"Resized '{strImgFileName}' to '{destImgFileName}'.");
            }

            BindingImgInsertTab();
        }

        /// <summary>
        /// Use datasource of the connection string to re-initialize folders
        /// if have not existed
        /// </summary>
        /// <returns>True, if verified or updated the application settings; Otherwise, False.</returns>
        private bool VerifyAndSetupDataSourceAndWorkingFolders()
        {
            var strNewConnectionString = string.Empty;
            var strACCDBFileName = string.Empty;
            var strImgFolderPath = string.Empty;
            var strImgDoneFolderPath = string.Empty;
            var strImgArchiveFolderPath = string.Empty;
            string strErrorFolder;

            var updateConnectionStringInAppSetting = false;
            var updateAndCreateImageFolder = false;
            var updateAndCreateDoneImageFolder = false;
            var updateAndCreateArchiveImageFolder = false;

            // find error log folder, configure if not existed
            if (!Directory.Exists(ConfigurationManager.AppSettings.Get("ErrLogPath")))
            {
                // Setup error folder and error file to log data
                strErrorFolder = GetAFolderLocation(@"ERROR FOLDER");
                if (string.IsNullOrWhiteSpace(strErrorFolder))
                    return false;
                strErrorFolder += @"\";
            }
            else
                strErrorFolder = ConfigurationManager.AppSettings.Get("ErrLogPath");

            Directory.CreateDirectory(strErrorFolder);
            var errorLogFile = ConfigurationManager.AppSettings.Get("ErrLogFile");    
            if (string.IsNullOrEmpty(errorLogFile))
                errorLogFile = @"ErrLog.txt";

            AddUpdateAppSettings(@"ErrLogPath", strErrorFolder);
            AddUpdateAppSettings(@"ErrLogFile", errorLogFile);

            _logFile = $"{ConfigurationManager.AppSettings.Get("ErrLogPath")}Log.txt";
            _errorFile = $"{ConfigurationManager.AppSettings.Get("ErrLogPath")}{ConfigurationManager.AppSettings.Get("ErrLogFile")}";
            Util.LogAMessage(_errorFile, "CDN_HL application restarts...");
            Util.LogAMessage(_logFile, "CDN_HL application restarts...");

            // find data source folder and configure it if not existed
            foreach (string part in Properties.Settings.Default.DN_HLConnectionString.Split(';'))
            {
                if (part.Trim().StartsWith("Data Source="))
                {
                    strNewConnectionString = $"{strNewConnectionString}Data Source=";
                    strACCDBFileName = part.Replace("Data Source=", "");
                    break;
                }
                strNewConnectionString = $"{strNewConnectionString}{part};";
            }
            var fileExisted = File.Exists(strACCDBFileName);
            if (!fileExisted)
            {
                Util.LogAMessage(_logFile, "accdb data source in connection string does not existed.");
                strACCDBFileName = GetACCDBFileLocation();
                if (string.IsNullOrWhiteSpace(strACCDBFileName))
                {
                    Util.LogAMessage(_logFile, "User terminated. Unable to setup new accdb data source.");
                    return false;
                }

                strNewConnectionString = $"{strNewConnectionString}{strACCDBFileName};";
                updateConnectionStringInAppSetting = true;
                Util.LogAMessage(_logFile, "Updated accdb data source.");
            }

            // find image, image done, archive folders, configure if not existed
            if (!Directory.Exists(ConfigurationManager.AppSettings.Get("ImgFolderPath")))
            {
                strImgFolderPath = GetAFolderLocation(@"IMAGE FOLDER");
                if (string.IsNullOrWhiteSpace(strImgFolderPath))
                {
                    Util.LogAMessage(_logFile, "User terminated. Unable to setup IMAGE FOLDER.");
                    return false;
                }
                updateAndCreateImageFolder = true;

                strImgDoneFolderPath = GetAFolderLocation(@"IMAGE DONE FOLDER");
                if (string.IsNullOrWhiteSpace(strImgDoneFolderPath))
                {
                    Util.LogAMessage(_logFile, "User terminated. Unable to setup IMAGE DONE FOLDER.");
                    return false;
                }
                updateAndCreateDoneImageFolder = true;

                strImgArchiveFolderPath = GetAFolderLocation(@"IMAGE ARCHIVE FOLDER");
                if (string.IsNullOrWhiteSpace(strImgArchiveFolderPath))
                {
                    Util.LogAMessage(_logFile, "User terminated. Unable to setup IMAGE ARCHIVE FOLDER.");
                    return false;
                }
                updateAndCreateArchiveImageFolder = true;
            }

            // Time to update app.setting or create all nessessary folders
            if (updateConnectionStringInAppSetting)
                UpdateConnectionString(strNewConnectionString);

            if (updateAndCreateImageFolder)
            {
                Directory.CreateDirectory(strImgFolderPath);
                AddUpdateAppSettings(@"ImgFolderPath", $"{strImgFolderPath}\\");
            }

            if (updateAndCreateDoneImageFolder)
            {
                Directory.CreateDirectory(strImgDoneFolderPath);
                AddUpdateAppSettings(@"ImgFolderDonePath", $"{strImgDoneFolderPath}\\");
            }

            if (updateAndCreateArchiveImageFolder)
            {
                Directory.CreateDirectory(strImgArchiveFolderPath);
                AddUpdateAppSettings(@"ImgFolderArchivePath", $"{strImgArchiveFolderPath}\\");
                Directory.CreateDirectory($"{strImgArchiveFolderPath}2");
                AddUpdateAppSettings(@"ImgFolderArchivePath2", $"{strImgArchiveFolderPath}2\\");
                Directory.CreateDirectory($"{strImgArchiveFolderPath}3");
                AddUpdateAppSettings(@"ImgFolderArchivePath3", $"{strImgArchiveFolderPath}3\\");
                Directory.CreateDirectory($"{strImgArchiveFolderPath}4");
                AddUpdateAppSettings(@"ImgFolderArchivePath4", $"{strImgArchiveFolderPath}4\\");
            }

            Util.LogAMessage(_logFile, "Verification/setting up/updating application settings completed.");
            return true;
        }

        /// <summary>
        /// Get access database file
        /// </summary>
        /// <returns></returns>
        private string GetACCDBFileLocation()
        {
            using (var openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Title = @"Select Database File";
                openFileDialog.InitialDirectory = @"C:\\";
                openFileDialog.Filter = "ACCDB files (*.accdb)|*.accdb";
                openFileDialog.RestoreDirectory = true;
                openFileDialog.Multiselect = false;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                    return openFileDialog.FileName;
                return string.Empty;
            }
        }

        /// <summary>
        /// Get a folder location
        /// </summary>
        /// <param name="folderType">string indicates what type of folder to be browsed</param>
        /// <returns>A folder path</returns>
        private string GetAFolderLocation(string folderType)
        {
            using (var folderBrowserDialog = new FolderBrowserDialog())
            {
                folderBrowserDialog.Description = $"Where {folderType} should be?";
                folderBrowserDialog.RootFolder = Environment.SpecialFolder.MyComputer;

                if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
                    return folderBrowserDialog.SelectedPath;
                return string.Empty;
            }
        }

        /// <summary>
        /// Add or update AppSettings
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="value">Value</param>
        private void AddUpdateAppSettings(string key, string value)
        {
            try
            {
                var configFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                var settings = ((AppSettingsSection)configFile.GetSection("appSettings")).Settings;
                if (settings[key] == null)
                    settings.Add(key, value);
                else
                    settings[key].Value = value;
                configFile.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection(configFile.AppSettings.SectionInformation.Name);
            }
            catch (ConfigurationErrorsException)
            {
                Util.LogAMessage(_errorFile, $"Failed to update the application setting for Key: '{key}', Value: '{value}'.");
            }
        }

        /// <summary>
        /// Update the connection string
        /// </summary>
        /// <param name="newConnectionString"></param>
        private void UpdateConnectionString(string newConnectionString)
        {
            var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            var connectionStringsSection = (ConnectionStringsSection)config.GetSection("connectionStrings");
            if (connectionStringsSection != null)
            {
                connectionStringsSection.ConnectionStrings["CDN_HL.Properties.Settings.DN_HLConnectionString"].ConnectionString = newConnectionString;
                config.Save();
                ConfigurationManager.RefreshSection("connectionStrings");
            }
        }

        private int rowIndex = 0;
        private void datasGridView_CellMouseUp(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                datasGridView.Rows[e.RowIndex].Selected = true;
                rowIndex = e.RowIndex;
                datasGridView.CurrentCell = datasGridView.Rows[e.RowIndex].Cells[1];
                dataGridMenuStrip.Show(datasGridView, e.Location);
                dataGridMenuStrip.Show(Cursor.Position);
            }
        }

        private void dataGridMenuStrip_Click(object sender, EventArgs e)
        {
            var hlFileName = string.Empty;
            try
            {
                if (!datasGridView.Rows[rowIndex].IsNewRow)
                {
                    var result = MessageBox.Show(@"Are you sure to delete this HL?", @"Deleting HL", MessageBoxButtons.OKCancel);
                    if (result == DialogResult.OK)
                    {
                        hlFileName = datasGridView.SelectedRows[0].Cells["HinhFileNamePath"].Value.ToString();
                        datasGridView.Rows.RemoveAt(rowIndex);
                        tblHLTableAdapter.Update(dN_HLDataSet.tblHL);  //Update the HL_DB
                        dN_HLDataSet.tblHL.AcceptChanges();                 //Update the DataSet
                        tblHLBindingSource.EndEdit();
                        tblHLBindingSource.ResetBindings(true);
                        datasGridView.Focus();
                        File.Delete($"{_ImgFolderDonePath}{hlFileName}");
                        Util.LogAMessage(_logFile, $"Deleted '{_ImgFolderDonePath}{hlFileName}'.");

                        DisplayImage();                                 //display the current selected item in datasGridView
                        txtsSearch.Text = "Successfully deleted!";
                        txtsSearch.ForeColor = Color.Red;          //Text 
                        txtsSearch.BackColor = Color.Yellow;       //Background
                    }
                }
            }
            catch (Exception ex)
            {
                Util.LogAMessage(_errorFile, $"Failed to delete '{_ImgFolderDonePath}{hlFileName}'. Exception: '{ex.Message}'");
            }
        }

        private string _strSourceImagesFolder = string.Empty;
        private void btnSourceFolder_Click(object sender, EventArgs e)
        {
            try
            {
                _strSourceImagesFolder = GetAFolderLocation(@"THE SOURCE IMAGES");
                if (string.IsNullOrWhiteSpace(_strSourceImagesFolder))
                    return;

                if (lstSourceImages.Items.Count > 0)
                    lstSourceImages.Items.Clear();

                foreach (var strImageName in Util.SearchFileName(_strSourceImagesFolder, "*.jpg|*.png"))
                    lstSourceImages.Items.Add(strImageName);

                if (lstSourceImages.Items.Count > 0)
                    lstSourceImages.SelectedIndex = 0;
                else
                    lblErrorMessage.Text = @"No .jpg or .png image in destination folder. Please reload source images.";
            }
            catch (Exception ex)
            {
                lblErrorMessage.Text = $"Unable to load source image files. Ex: {ex.Message}";
            }

            SelectDestinationFolder();
            if (!lblErrorMessage.Text.Contains(@"Warning"))
                lblErrorMessage.Text = @"Ready...";
        }

        private void MigrateImages()
        {
            var completedImagesCount = 0;
            var failedImagesCount = 0;
            var timer = new Stopwatch();
            timer.Start();
            lstDestImages.Items.Clear();
            lblErrorMessage.Text = @"Ready...";

            var selectedItems = lstSourceImages.SelectedItems;
            for (int i = selectedItems.Count - 1; i >= 0; i--)
            {
                var toDoItem = selectedItems[i].ToString();
                try
                {
                    var sourceFile = $"{_strSourceImagesFolder}{toDoItem}";
                    var destFile = $"{lblDestImageFolder.Text}{toDoItem}";
                    var result = Util.ResizeImageAndSave(sourceFile, destFile);
                    if (result == false)
                    {
                        lblErrorMessage.Text = @"Failed. See error log for details.";
                        Util.LogAMessage(_errorFile, $"Failed to resize '{sourceFile}' to '{destFile}'." +
                            Environment.NewLine + $"File may have already existed in the destination folder.");
                        failedImagesCount++;
                        continue;
                    }

                    lstDestImages.Items.Add(selectedItems[i]);
                    lstSourceImages.Items.Remove(selectedItems[i]);
                    completedImagesCount++;
                }
                catch (Exception ex)
                {
                    lblErrorMessage.Text = @"Failed. See error log for details.";
                    Util.LogAMessage(_errorFile, $"Failed to resize '{_strSourceImagesFolder}{toDoItem}' to '{lblDestImageFolder.Text}{toDoItem}'." +
                        Environment.NewLine + $"Ex: {ex.Message}");
                    failedImagesCount++;
                    continue;
                }
            }

            timer.Stop();
            Util.LogAMessage(_logFile, $"Resized image operation '{_strSourceImagesFolder}' to '{lblDestImageFolder.Text}' completed." +
                Environment.NewLine + $"Resized: '{completedImagesCount}', Failed: '{failedImagesCount}', " +
                $"Time: '{timer.Elapsed}'.");

            lblErrorMessage.Text = $"Operation completed. " +
                $"Resized: '{completedImagesCount}', Failed: '{failedImagesCount}', " +
                $"Time: '{timer.Elapsed}'. " +
                $"See log file for details!!!";
        }

        private void btnResize1_Click(object sender, EventArgs e)
        {
            var doNotStart = false;
            if (lstSourceImages.Items.Count < 1 || lstSourceImages.SelectedIndex == -1)
            {
                lblErrorMessage.Text = @"No source file | file(s) selected";
                doNotStart = true;
            }
            if (lblDestImageFolder.Text == @".....")
            { 
                lblErrorMessage.Text += @" | Select destination folder";
                doNotStart = true;
            }

            if (doNotStart)
                return;

            MigrateImages();
        }

        private void btnResizeAll_Click(object sender, EventArgs e)
        {
            if (lstSourceImages.Items.Count < 1 ||  lblDestImageFolder.Text == @".....")
            {
                lblErrorMessage.Text = @"No source file destination has not configured.";
                return;
            }

            for (int i = 0; i < lstSourceImages.Items.Count; i++)
                lstSourceImages.SetSelected(i, true);

            MigrateImages();
        }

        private void SelectDestinationFolder()
        {
            var strDestImagesFolder = GetAFolderLocation(@"THE RESIZED IMAGES");
            if (string.IsNullOrWhiteSpace(strDestImagesFolder))
            {
                lblErrorMessage.Text = @"Select destination folder";
                return;
            }

            lblDestImageFolder.Text = $"{strDestImagesFolder}";

            try
            {
                if (lstDestImages.Items.Count > 0)
                    lstDestImages.Items.Clear();

                foreach (var strImageName in Util.SearchFileName(strDestImagesFolder, "*.jpg|*.png"))
                    lstDestImages.Items.Add(strImageName);

                if (lstDestImages.Items.Count > 0)
                    lblErrorMessage.Text = @"Warning: there are existing images in the destination folder.";
            }
            catch (Exception ex)
            {
                lblErrorMessage.Text = $"Unable to load the destination image files. Ex: {ex.Message}";
            }
        }

        private void btnSelectDest_Click(object sender, EventArgs e)
        {
            SelectDestinationFolder();
        }

        #endregion =================== new methods
    }
}
