using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace PassMan
{
    public partial class FormMain : Form
    {
        public static string newfilename = null;
        string pathFiles = pathAddSlash(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)) + @"XXX\";
        string userPassword = null;
        string currentFile = null;
        string previousFile = null;
        const int derivationIterations = 1000;
        const int keySize = 256;
        int ylocation = 0;
        int count = 0;
        bool changedText = false;

        public FormMain()
        {
            InitializeComponent();
            if (!Directory.Exists(pathFiles))
            {
                try
                {
                    Directory.CreateDirectory(pathFiles);
                }
                catch
                {
                    MessageBox.Show("Ошибка создания папки Files");
                }
            }
        }

        private void textBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                userPassword = textBox1.Text;
                textBox1.Visible = false;
                textBox2.Visible = true;
                label1.Visible = true;
                panel1.Visible = true;
                button1.Visible = true;
                Size = new Size(780, 400);
                Location = new Point((Screen.PrimaryScreen.WorkingArea.Width - this.Width) / 2, (Screen.PrimaryScreen.WorkingArea.Height - this.Height) / 2);
                foreach (string line in Directory.GetFiles(pathFiles, "*.xxx"))
                {
                    creeateButton(line.Remove(0, pathFiles.Length));
                }
            }
        }

        private void creeateButton(string name)
        {
            Button myButton = new Button();
            myButton.Text = name;
            myButton.Font = new System.Drawing.Font("Arial", 9.75F, FontStyle.Regular);
            myButton.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            myButton.Location = new System.Drawing.Point(0, ylocation);
            myButton.Size = new System.Drawing.Size(120, 32);
            myButton.Click += newButton;
            panel1.Controls.Add(myButton);
            ylocation += 35;
            count++;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var form = new FormMessage();
            form.ShowDialog();
            form = null;
            if (!String.IsNullOrEmpty(newfilename) && !File.Exists(pathFiles + newfilename))
            {
                writeToFile(pathFiles + newfilename, new List<string>() { encryptString("PassMan Test OK", userPassword) });
                creeateButton(newfilename);
            }
        }

        private void newButton(object sender, EventArgs e)
        {
            Button clickedButton = (Button)sender;
            currentFile = clickedButton.Text;
            label1.Text = currentFile;
            if (previousFile != null && changedText)
            {
                List<string> writeList = new List<string>();
                writeList.Add(encryptString("PassMan Test OK", userPassword));
                foreach (string line in textBox2.Lines)
                {
                    if (!String.IsNullOrEmpty(line))
                    {
                        writeList.Add(encryptString(line, userPassword));
                    }
                }
                writeToFile(pathFiles + previousFile, writeList);
                writeList.Clear();
                changedText = false;
            }
            if (currentFile != previousFile && File.Exists(pathFiles + currentFile))
            {
                textBox2.TextChanged -= textBox2_TextChanged;
                textBox2.Clear();
                List<string> cacheFile = new List<string>(File.ReadAllLines(pathFiles + currentFile));
                if (cacheFile.Count > 0 && decryptString(cacheFile[0], userPassword) == "PassMan Test OK")
                {
                    List<string> cacheList = new List<string>();
                    for (int i = 1; i < cacheFile.Count; i++)
                    {
                        cacheList.Add(decryptString(cacheFile[i], userPassword));
                    }
                    textBox2.AppendText(String.Join(Environment.NewLine, cacheList));
                    textBox2.SelectionStart = 0;
                    textBox2.ScrollToCaret();
                    textBox2.ReadOnly = false;
                }
                else
                {
                    textBox2.AppendText("Файл пуст или неверный пароль!");
                    textBox2.ReadOnly = true;
                }
                textBox2.TextChanged += textBox2_TextChanged;
            }
            previousFile = currentFile;
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            changedText = true;
            label1.Text = "* " + currentFile;
        }

        private string encryptString(string plainText, string passPhrase)
        {
            try
            {
                var saltStringBytes = generate256BitsOfRandomEntropy();
                var ivStringBytes = generate256BitsOfRandomEntropy();
                var plainTextBytes = Encoding.UTF8.GetBytes(plainText);
                using (var password = new Rfc2898DeriveBytes(passPhrase, saltStringBytes, derivationIterations))
                {
                    var keyBytes = password.GetBytes(keySize / 8);
                    using (var symmetricKey = new RijndaelManaged())
                    {
                        symmetricKey.BlockSize = 256;
                        symmetricKey.Mode = CipherMode.CBC;
                        symmetricKey.Padding = PaddingMode.PKCS7;
                        using (var encryptor = symmetricKey.CreateEncryptor(keyBytes, ivStringBytes))
                        {
                            using (var memoryStream = new MemoryStream())
                            {
                                using (var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                                {
                                    cryptoStream.Write(plainTextBytes, 0, plainTextBytes.Length);
                                    cryptoStream.FlushFinalBlock();
                                    var cipherTextBytes = saltStringBytes;
                                    cipherTextBytes = cipherTextBytes.Concat(ivStringBytes).ToArray();
                                    cipherTextBytes = cipherTextBytes.Concat(memoryStream.ToArray()).ToArray();
                                    memoryStream.Close();
                                    cryptoStream.Close();
                                    return Convert.ToBase64String(cipherTextBytes);
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
                return "";
            }
        }

        private string decryptString(string cipherText, string passPhrase)
        {
            try
            {
                var cipherTextBytesWithSaltAndIv = Convert.FromBase64String(cipherText);
                var saltStringBytes = cipherTextBytesWithSaltAndIv.Take(keySize / 8).ToArray();
                var ivStringBytes = cipherTextBytesWithSaltAndIv.Skip(keySize / 8).Take(keySize / 8).ToArray();
                var cipherTextBytes = cipherTextBytesWithSaltAndIv.Skip((keySize / 8) * 2).Take(cipherTextBytesWithSaltAndIv.Length - ((keySize / 8) * 2)).ToArray();
                using (var password = new Rfc2898DeriveBytes(passPhrase, saltStringBytes, derivationIterations))
                {
                    var keyBytes = password.GetBytes(keySize / 8);
                    using (var symmetricKey = new RijndaelManaged())
                    {
                        symmetricKey.BlockSize = 256;
                        symmetricKey.Mode = CipherMode.CBC;
                        symmetricKey.Padding = PaddingMode.PKCS7;
                        using (var decryptor = symmetricKey.CreateDecryptor(keyBytes, ivStringBytes))
                        {
                            using (var memoryStream = new MemoryStream(cipherTextBytes))
                            {
                                using (var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
                                {
                                    var plainTextBytes = new byte[cipherTextBytes.Length];
                                    var decryptedByteCount = cryptoStream.Read(plainTextBytes, 0, plainTextBytes.Length);
                                    memoryStream.Close();
                                    cryptoStream.Close();
                                    return Encoding.UTF8.GetString(plainTextBytes, 0, decryptedByteCount);
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
                return "";
            }
        }

        private byte[] generate256BitsOfRandomEntropy()
        {
            var randomBytes = new byte[32];
            using (var rngCsp = new RNGCryptoServiceProvider())
            {
                rngCsp.GetBytes(randomBytes);
            }
            return randomBytes;
        }

        private void textBox2_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.A)
            {
                if (sender != null)
                    ((TextBox)sender).SelectAll();
            }
        }

        private void writeToFile(string path, List<string> list)
        {
            try
            {
                File.WriteAllLines(path, list, new UTF8Encoding(false));
            }
            catch
            {
                MessageBox.Show("Ошибка записи в файл:" + path);
            }
        }

        private static string pathAddSlash(string path)
        {
            if (path.EndsWith(@"/") || path.EndsWith(@"\"))
            {
                return path;
            }
            else if (path.Contains(@"/"))
            {
                return path + @"/";
            }
            else if (path.Contains(@"\"))
            {
                return path + @"\";
            }
            return path;
        }
    }
}