using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ShiftJisDecoder
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }


        public static double isJapanese(string str)
        {
            const string jcharacter = "ぁあぃいぅうぇえぉおかがきぎくぐけげこごさざしじすずせぜそぞただちぢっつづてでとどなにぬねのはばひびぴふぶぷへべぺほぼぽまみむめもゃやゅゆょよらりるれろゎわゐゑをん゛゜ゝゞァアィイゥウェエォオカガキギクグケゲコゴサザシジスズセゼソゾタダチヂッツヅテデトドナニヌネノハバパヒビピフブプヘベペホボポマミムメモャヤュユョヨラリルレロヮワヰヱヲンヴヵヶーヽヾぁあぃいぅうぇえぉおかがきぎくぐけげこごさざしじすずせぜそぞただちぢっつづてでとどなにぬねのはばひびぴふぶぷへべぺほぼぽまみむめもゃやゅゆょよらりるれろゎわゐゑをんﾞﾟゝゞｧｱｨｲｩｳｪｴｫｵｶﾞｷｸｹｺｻｼｽｾｿﾀﾁｯﾂﾃﾄﾅﾆﾇﾈﾉﾊﾋﾌﾟﾍﾎﾏﾐﾑﾒﾓｬﾔｭﾕｮﾖﾗﾘﾙﾚﾛﾜｲｴｦﾝ";
            double sum = 0;
            foreach (var c in str)
            {
                if (jcharacter.Contains(c)) sum++;
            }
            return sum / str.Length;
        }

        /// <summary>
        /// 将错误编码的日文乱码转化为正确的日文
        /// </summary>
        /// <param name="errcode"></param>
        /// <param name="errEncoding"></param>
        /// <returns></returns>
        private string convertJEoncode(string errcode, Encoding errEncoding = null)
        {
            if (isJapanese(errcode)>=0.001)
            {
                //已经被转成日文了，不用转了
                return errcode;
            }

            if (errEncoding == null) errEncoding = Encoding.GetEncoding("gb2312");

            var hopefullyRecovered = errEncoding.GetBytes(errcode);
            var oughtToBeJapanese = Encoding.GetEncoding(932).GetString(hopefullyRecovered);

            return oughtToBeJapanese;
        }

        private static string removeInvalidChar(string ori)
        {
            var invalid = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
            foreach (char c in invalid) ori = ori.Replace(c.ToString(), "_");
            return ori;
        }

        private string getEnableFilename(string path)
        {
            if (!File.Exists(path) && !Directory.Exists(path)) return path;
            int rename = 1;
            string dict = Path.GetDirectoryName(path);
            string name = Path.GetFileNameWithoutExtension(path);
            string ext = Path.GetExtension(path);
            while (File.Exists(path) || Directory.Exists(path))
            {
                path = string.Format("{0}/{1}({2}){3}", dict, name, rename++, ext);
            }
            return path;
        }

        private void dealOneFile(string path,string dict)
        {
            bool deal_text = checkBox2.Checked;

            //print("load:" + path);

            string filename = Path.GetFileNameWithoutExtension(path);
            string ext = Path.GetExtension(path);
            // string dict = Path.GetDirectoryName(path);

            string out_filename = removeInvalidChar(convertJEoncode(filename));
            string out_path = dict + "/" + out_filename + ext;
            out_path = getEnableFilename(out_path);

            if (deal_text && text_types.Contains(ext))
            {
                // decoding content text

                string out_filecontent = "";
                string tmp1 = File.ReadAllText(path, Encoding.GetEncoding(932));
                string tmp2 = File.ReadAllText(path, Encoding.GetEncoding("gb2312"));
                string tmp3 = File.ReadAllText(path, Encoding.UTF8);
                double conf1 = isJapanese(tmp1);
                double conf2 = isJapanese(tmp2);
                double conf3 = isJapanese(tmp3);
                if (conf1>conf2 && conf1>conf3) out_filecontent = tmp1;
                else if (conf2 > conf1 && conf2 > conf3) out_filecontent = tmp2;
                else if (conf3 > conf2 && conf3 > conf1) out_filecontent = tmp3;

                if (out_filecontent.Length <= 0) out_filecontent = tmp2;

                File.WriteAllText(out_path, out_filecontent);
            }
            else
            {
                //copy
                File.Copy(path, out_path);
            }
        }

        private string[] text_types;
        private void readConfig()
        {
            string filename = "config.ini";
            if (!File.Exists(filename))
            {
                var f = File.Create(filename);
                f.Dispose();
                File.WriteAllLines(filename, new string[] { ".txt", ".ini", ".html", ".js" });
            }
            text_types = File.ReadAllLines(filename, Encoding.UTF8);
        }

        private void dealFiles(string[] files,string dict)
        {
            readConfig();
            bool deal_dir = checkBox1.Checked;
            

            foreach (var path in files)
            {
                if (string.IsNullOrEmpty(dict)) dict = Path.GetDirectoryName(path);

                if(Directory.Exists(path) && deal_dir)
                {
                    // deal dir
                    string dict_name = Path.GetFileName(path);
                    dict_name= removeInvalidChar(convertJEoncode(dict_name));
                    string out_dict_name = getEnableFilename(dict + "/" + dict_name)+"/";
                    Directory.CreateDirectory(out_dict_name);
                    List<string> all_next_files=new List<string>();
                    string[] next_dicts = Directory.GetDirectories(path);
                    string[] next_files = Directory.GetFiles(path);
                    foreach (var f in next_dicts) all_next_files.Add(f);
                    foreach (var f in next_files) all_next_files.Add(f);
                    // 递归
                    dealFiles(all_next_files.ToArray(), Path.GetDirectoryName(out_dict_name));
                }
                else
                {
                    // deal simple file
                    if(File.Exists(path)) dealOneFile(path,dict);
                }
                
            }
        }


        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            var badstringFromDatabase = textBox1.Text;
            var hopefullyRecovered = Encoding.GetEncoding("gb2312").GetBytes(badstringFromDatabase);
            var oughtToBeJapanese = Encoding.GetEncoding(932).GetString(hopefullyRecovered);
            textBox2.Text = oughtToBeJapanese;
        }

        private void Form1_DragDrop(object sender, DragEventArgs e)
        {
            try
            {
                string[] s = (string[])e.Data.GetData(DataFormats.FileDrop, false);
                if (s.Length > 0)
                {
                    dealFiles(s,null);
                }
            }
            catch (Exception ex)
            {
                //print(ex.Message);
            }
        }

        private void Form1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.All;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }
    }
}
