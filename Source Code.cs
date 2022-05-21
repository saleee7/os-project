using System;
using System.Collections.Generic;
using System.IO;

namespace Mini_FAT_System
{
  class Virtual_Disk
    {

        public static void Initialize()
        {

            if (!File.Exists("Virtual Disk.txt"))
            {
                using (FileStream stream = new FileStream("Virtual Disk.txt", FileMode.Create, FileAccess.ReadWrite))
                {
                    for (int j = 0; j < 1024; j++)
                        stream.WriteByte((byte)'0');

                    for (int i = 0; i < 4; i++)
                        for (int j = 0; j < 1024; j++)
                            stream.WriteByte((byte)'*');

                    for (int i = 0; i < 1019; i++)
                        for (int j = 0; j < 1024; j++)
                            stream.WriteByte((byte)'#');

                    FAT_Table.Initialize_FAT();
                    Directory root = new Directory("C:".ToCharArray(), 0x10, 5);
                   
                    stream.Close();
                    root.Write_Directory();
                    
                    FAT_Table.Write_FAT();
                    Program.current_dir = root;
                }
            }

            else
            {
                FAT_Table.Read_FAT();
                Directory root = new Directory("C:".ToCharArray(), 0x10, 5);
                if (FAT_Table.Get_FAT_Value(5) != 0)
                    root.Read_Directory();
                Program.current_dir = root;               

            }


        }

        public static void Write_Cluster(byte[] cluster, int index)
        {
            using (FileStream file = new FileStream("Virtual Disk.txt", FileMode.Open, FileAccess.ReadWrite))
            {

                file.Seek(index * 1024, SeekOrigin.Begin);

                for (int i = 0; i < 1024; i++)
                    file.WriteByte(cluster[i]);

            }

        }
        public static byte[] Read_Cluster(int index)
        {
            using (FileStream file = new FileStream("Virtual Disk.txt", FileMode.Open, FileAccess.ReadWrite))
            {
                byte[] cluster = new byte[1024];

                file.Seek(1024 * index, SeekOrigin.Begin);
                file.Read(cluster, 0, 1024);

                return cluster;

            }
        }
    }

  class FAT_Table
    {
        static int[] FAT = new int[1024];

        public static void Initialize_FAT()
        {
            FAT[0] = -1;FAT[1] = 2;FAT[2] = 3;FAT[3] = 4;FAT[4] = -1;
            for (int i = 5; i < FAT.Length; ++i)
                FAT[i] = 0;
        }

        public static void Write_FAT()
        {
            using (FileStream file = new FileStream("Virtual Disk.txt", FileMode.Open, FileAccess.Write))
            {
                byte[] FAT_in_bytes = new byte[4096];
                Buffer.BlockCopy(FAT, 0, FAT_in_bytes, 0, FAT.Length);

                file.Seek(1024, SeekOrigin.Begin);

                for (int i = 0; i < FAT_in_bytes.Length; i++)
                    file.WriteByte(FAT_in_bytes[i]);

                file.Close();

            }
        }


        public static void Read_FAT()
        {
            using (FileStream file = new FileStream("Virtual Disk.txt", FileMode.Open, FileAccess.Read))
            {
                byte[] ReadFat = new byte[4096];

                file.Seek(1024, SeekOrigin.Begin);

                file.Read(ReadFat, 0, 4096);

                Buffer.BlockCopy(ReadFat, 0, FAT, 0, 4096);

                file.Close();
            }
        }


        public static int Get_FreeCluster()
        {
           
            for (int i = 0; i < FAT.Length; i++)
            {
                if (FAT[i] == 0)
                   return i;                    
            }

            return -1;
        }

        public static int Get_FAT_Value(int index)
        {
            return FAT[index];
        }

        public static void Set_FAT_Value(int index, int value)
        {
            FAT[index] = value;
        }

        public static int Get_AvailbaleClusters()
        {
            int counter = 0;

            for (int i = 0; i < FAT.Length; i++)
            {
                if (FAT[i] == 0)
                    counter += 1;

            }

            return counter;
        }

        public static int Get_FreeSpace()
        {
            return (Get_AvailbaleClusters() * 1024);
        }

    }

  class Directory_Entry
    {
        public char[] name = new char[11];
        public byte attribute;
        byte[] empty = new byte[12];
        public int size;
        public int first_cluster;

        Directory_Entry() { }

        public Directory_Entry(char[] name, byte attribute, int first_cluster, int size = 0)
        {
            if (name.Length > 11)
                for (int i = 0; i < 11; i++)
                    this.name[i] = name[i];

            else
                this.name = name;

            this.attribute = attribute;
            this.first_cluster = first_cluster;
            this.size = size;

        }

        public byte[] Get_Bytes_of_DirEntry()//get_Bytes()
        {
            byte[] tempBytes = new byte[32];
            int[] tempInt = new int[1];

            for (int i = 0; i < name.Length; i++)
                tempBytes[i] = (byte)name[i];

            tempBytes[11] = attribute;
            Buffer.BlockCopy(empty, 0, tempBytes, 12, empty.Length);

            tempInt[0] = first_cluster;
            Buffer.BlockCopy(tempInt, 0, tempBytes, 24, 1);

            tempInt[0] = size;
            Buffer.BlockCopy(tempInt, 0, tempBytes, 28, 1);

            return tempBytes;

        }

        public Directory_Entry Read_Directory_Entry()
        {
            Directory_Entry obj = new Directory_Entry();

            obj.name = this.name;

            obj.attribute = this.attribute;

            obj.empty = this.empty;

            obj.first_cluster = this.first_cluster;

            obj.size = this.size;

            return obj;
        }

        public Directory_Entry Read_Directory_Entry(byte[] b)//b-->dir_entry
        {
            Directory_Entry obj = new Directory_Entry();

            int[] tempInt = new int[1];


            for (int i = 0; i < 11; i++)
                obj.name[i] = (char)b[i];

            obj.attribute = b[11];


            Buffer.BlockCopy(b, 12, obj.empty, 0, obj.empty.Length);

            Buffer.BlockCopy(b, 24, tempInt, 0, 4);
            obj.first_cluster = tempInt[0];

            Buffer.BlockCopy(b, 28, tempInt, 0, 4);
            obj.size = tempInt[0];

            return obj;
        }

    }

  class Directory : Directory_Entry
    {
        public List<Directory_Entry> Directory_table = new List<Directory_Entry>();

        public Directory Parent;

        public Directory(char[] n, byte attri, int clust, Directory Par = null) : base(n, attri, clust)
        {

            Parent = Par;
        }

        public void Write_Directory()
        {
            
            byte[] DTB = new byte[32 * Directory_table.Count];
            byte[] DEB = new byte[32];

            for (int i = 0; i < Directory_table.Count; i++)
            {

                DEB = Directory_table[i].Get_Bytes_of_DirEntry();

                for (int j = i * 32, c = 0; c < 32; j++, c++)
                {
                    DTB[j] = DEB[c];
                }

            }

            double num_of_req_blocks = Math.Ceiling(DTB.Length / 1024.0);

            if (num_of_req_blocks <= FAT_Table.Get_FreeSpace())
            {
                int FI;
                int LI = -1;

                if (first_cluster != 0)
                    FI = first_cluster;

                else
                {

                    FI = FAT_Table.Get_FreeCluster();
                    first_cluster = FI;
                }


                byte[] cluster = new byte[1024];

                for (int i = 0; i < num_of_req_blocks; i++)
                {
                    for (int j = i * 1024, c = 0; c < 1024; j++, c++)
                    {

                        if (j < DTB.Length)
                            cluster[c] = DTB[j];

                        else
                            cluster[c] = (byte)'*';

                    }

                    Virtual_Disk.Write_Cluster(cluster, FI);
                    FAT_Table.Set_FAT_Value(FI, -1);

                    if (LI != -1)
                        FAT_Table.Set_FAT_Value(LI, FI);


                    LI = FI;
                    FI = FAT_Table.Get_FreeCluster();

                }

                FAT_Table.Write_FAT();
            }


        }

        public void Read_Directory()
        {
            List<byte> bytes = new List<byte>();
            List<Directory_Entry> DataTable = new List<Directory_Entry>();

            int firstIndex;
            int Next;

            if (first_cluster != 0)
            {
                firstIndex = first_cluster;
                Next = FAT_Table.Get_FAT_Value(firstIndex);
                bytes.AddRange(Virtual_Disk.Read_Cluster(firstIndex));

                do
                {
                    firstIndex = Next;
                    if (firstIndex != -1)
                    {
                        bytes.AddRange(Virtual_Disk.Read_Cluster(firstIndex));
                        Next = FAT_Table.Get_FAT_Value(firstIndex);
                    }

                } while (Next != -1);


                byte[] dirEntry = new byte[32];

                for (int i = 0; i < bytes.Count;)
                {

                    for (int j = 0; j < 32; j++, i++)
                    {

                        dirEntry[j] = bytes[i];

                    }


                    DataTable.Add(Read_Directory_Entry(dirEntry));
                }

            }
            Directory_table = DataTable;

        }

        public int Search(string name)
        {
            name = name.TrimEnd(new char[] { '\0' });
            string str;

            for (int i = 0; i < Directory_table.Count; i++)
            {

                str = new string(Directory_table[i].name).TrimEnd(new char[] { '\0' });

                if (str == name)
                    return i;

            }

            return -1;

        }

        public void Update_Content(Directory_Entry d)
        {

            string folder_name = new string(d.name);

            Read_Directory();

            int index = Search(folder_name);
            if (index != -1)
            {
                Directory_table.RemoveAt(index);
                Directory_table.Insert(index, d);
            }
            Write_Directory();
        }

        public void Delete_Directory()
        {
            if (first_cluster != 0)
            {
                int index = first_cluster;
                int next = FAT_Table.Get_FAT_Value(index);

                do
                {
                    FAT_Table.Set_FAT_Value(index, 0);
                    index = next;
                    if (index != -1)
                        next = FAT_Table.Get_FAT_Value(index);

                } while (next != -1);

                FAT_Table.Write_FAT();
            }
            if (Parent != null)
            {
                string folder_name = new string(name);
                Parent.Read_Directory();
                int i = Parent.Search(folder_name);

                if (i != -1)
                {
                    Parent.Directory_table.RemoveAt(i);
                    Parent.Write_Directory();
                }
            }

        }
    }

  class File_Entry : Directory_Entry
    {
        public string content;
        Directory Parent;


        public File_Entry(char[] name, byte attribute, int firstcluster, int size, string content = "", Directory Parent = null) : base(name, attribute, firstcluster, size)
        {
            this.Parent = Parent;
            this.content = content;
        }


        public void Write_File()
        {

            double num_of_req_blocks = Math.Ceiling(content.Length / 1024.0);


            if (num_of_req_blocks <= FAT_Table.Get_FreeSpace())
            {
                int FI;
                int LI = -1;

                if (first_cluster != 0)
                    FI = first_cluster;

                else
                {
                    FI = FAT_Table.Get_FreeCluster();
                    first_cluster = FI;
                }


                byte[] cluster = new byte[1024];
                for (int i = 0; i < num_of_req_blocks; i++)
                {
                    for (int j = i * 1024, c = 0; c < 1024; j++, c++)
                    {

                        if (j < this.content.Length)
                            cluster[c] = (byte)this.content[j];

                        else
                            cluster[c] = (byte)'*';

                    }


                    Virtual_Disk.Write_Cluster(cluster, FI);
                    FAT_Table.Set_FAT_Value(FI, -1);

                    if (LI != -1)
                        FAT_Table.Set_FAT_Value(LI, FI);

                    LI = FI;

                    FI = FAT_Table.Get_FreeCluster();

                }
                FAT_Table.Write_FAT();
            }
        }

        public void Read_File()
        {
            List<byte> bytes = new List<byte>();

            int FI;
            int Next;

            if (first_cluster != 0)
            {
                FI = first_cluster;
                Next = FAT_Table.Get_FAT_Value(FI);
                bytes.AddRange(Virtual_Disk.Read_Cluster(FI));

                do
                {
                    FI = Next;
                    if (FI != -1)
                    {
                        bytes.AddRange(Virtual_Disk.Read_Cluster(FI));
                        Next = FAT_Table.Get_FAT_Value(FI);
                    }

                } while (Next != -1);



                for (int i = 0; i < bytes.Count; i++)
                {
                    this.content += (char)bytes[i];

                }

            }

        }

        public void Delete_File()
        {
            if (first_cluster != 0)
            {
                int index = first_cluster;
                int next = FAT_Table.Get_FAT_Value(index);

                do
                {
                    FAT_Table.Set_FAT_Value(index, 0);
                    index = next;
                    if (index != -1)
                        next = FAT_Table.Get_FAT_Value(index);

                } while (next != -1);

                FAT_Table.Write_FAT();
            }
            if (Parent != null)
            {
                string file_name = new string(name);
                Parent.Read_Directory();
                int i = Parent.Search(file_name);

                if (i != -1)
                {
                    Parent.Directory_table.RemoveAt(i);
                    Parent.Write_Directory();
                }
            }

        }

    }

  public class Commands
    {
        public static string CurrentDirectory;

        public static void help()
        {
            string[] lines = File.ReadAllLines(@"C:\Users\Ahmed Saleh\source\repos\Mini-FAT System\bin\Debug\help.txt");
            foreach (string line in lines)
                Console.WriteLine(line);

        }//

        public static void help(string arg)
        {
            string[] lines = File.ReadAllLines(@"C:\Users\Ahmed Saleh\source\repos\Mini-FAT System\bin\Debug\help.txt");
            foreach (string line in lines)
            {
                if (line.Contains(arg.ToUpper()))
                {
                    Console.WriteLine("\n");
                    Console.WriteLine(line + "\n");
                    break;
                }

            }
        }//

        public static void md(string name)
        {

            int index = Program.current_dir.Search(name);

            if (index == -1)
            {
                Directory_Entry d = new Directory_Entry(name.ToCharArray(), 0x10, 0);
                Program.current_dir.Directory_table.Add(d);
                Program.current_dir.Write_Directory();

                if (Program.current_dir.Parent != null)
                    Program.current_dir.Parent.Update_Content(Program.current_dir.Read_Directory_Entry());
            }

            else
            {
                Console.WriteLine($"\nA subdirectory or file {name} already exists.");
            }
        }//

        public static void rd(string name)
        {
            int index = Program.current_dir.Search(name);
            if (index != -1 && Program.current_dir.Directory_table[index].attribute == 0x10)
            {

                int Fc = Program.current_dir.Directory_table[index].first_cluster;
                Directory d = new Directory(name.ToCharArray(), 0x10, Fc, Program.current_dir);
                d.Delete_Directory();

            }
            else
                Console.WriteLine("\nThe system cannot find the directory specified.");

        }//

        public static void cd(string name)//
        {
            int index = Program.current_dir.Search(name);

            if (index != -1 && Program.current_dir.Directory_table[index].attribute == 0x10)
            {
                int Fc = Program.current_dir.Directory_table[index].first_cluster;
                Directory d = new Directory(name.ToCharArray(), 0x10, Fc, Program.current_dir);
                Program.current_dir = d;
                string str = new string(d.name).TrimEnd(new char[] { '\0' });
                Program.current_path = "\\" + str;
                Program.current_dir.Read_Directory();
                Commands.CurrentDirectory += Program.current_path;
                Console.WriteLine();

            }

            else
            {
                Console.WriteLine("\nThe system cannot find the path specified.\n");
            }

        }

        public static void dir()
        {
            
            Console.WriteLine($"\nDirectory of : {Commands.CurrentDirectory}\n");
            int file_counter = 0;
            int dir_counter = 0;
            int files_size = 0;

            for (int i = 0; i < Program.current_dir.Directory_table.Count; i++)
            {
                if (Program.current_dir.Directory_table[i].attribute == 0x0)
                {
                    string str = new string(Program.current_dir.Directory_table[i].name).TrimEnd(new char[] { '\0' });
                    Console.Write("{0,14:N0}", Program.current_dir.Directory_table[i].size);
                    Console.WriteLine("\t" + str);
                    file_counter++;
                    files_size += Program.current_dir.Directory_table[i].size;
                }

                else if (Program.current_dir.Directory_table[i].attribute == 0x10)
                {
                    string str = new string(Program.current_dir.Directory_table[i].name).TrimEnd(new char[] { '\0' });
                    Console.WriteLine($"<Dir>\t\t{str}");
                    dir_counter++;
                }
            }
            Console.WriteLine($"\n  \t\t{file_counter} File(s)\t{files_size} bytes");
            Console.WriteLine($"    \t\t{dir_counter} Dir(s)\t{FAT_Table.Get_FreeSpace()} bytes free");

        }//

        //public static void dir(string path)
        //{
        //    Commands.cd2(path);
        //    dir();
        //}

        public static void import(string path)
        {
            if (File.Exists(path))
            {

                var indexe = path.Split('\\');

                string name = indexe[indexe.Length - 1];
                string content = File.ReadAllText(path);
                int size = content.Length;
                int index = Program.current_dir.Search(name);
                int fc = 0;

                if (size > 0)
                    fc = FAT_Table.Get_FreeCluster();

                if (index == -1)
                {
                    File_Entry f = new File_Entry(name.ToCharArray(), 0x0, fc, size, content, Program.current_dir);
                    f.Write_File();
                    Directory_Entry d = new Directory_Entry(name.ToCharArray(), 0x0, fc, size);
                    Program.current_dir.Directory_table.Add(d);
                    Program.current_dir.Write_Directory();

                    if (Program.current_dir.Parent != null)
                        Program.current_dir.Parent.Update_Content(Program.current_dir.Read_Directory_Entry());

                    Console.WriteLine();
                }


                else
                    Console.WriteLine($"\nFile {name} already exists.\n");

            }
            else
                Console.WriteLine("\nThe system cannot find the path specified.\n");

        }

        public static void type(string name)
        {
            int index = Program.current_dir.Search(name);


            if (index != -1 && Program.current_dir.Directory_table[index].attribute == 0x0)
            {
                int fc = Program.current_dir.Directory_table[index].first_cluster;
                int size = Program.current_dir.Directory_table[index].size;
                string content = "";
                File_Entry f = new File_Entry(name.ToCharArray(), 0x0, fc, size, content, Program.current_dir);
                f.Read_File();
                content = f.content.TrimEnd(new char[] { '*' });
                Console.WriteLine("\n" + content + "\n");

            }

            else
                Console.WriteLine("\nThe system cannot find the file specified.\n");
        }//

        public static void cls()
        {
            Console.Clear();
        }//

        public static void export(string file_name, string destination)
        {
            int index = Program.current_dir.Search(file_name);

            if (index != -1 && Program.current_dir.Directory_table[index].attribute == 0x0)
            {
                if (System.IO.Directory.Exists(destination))
                {
                    int fc = Program.current_dir.Directory_table[index].first_cluster;
                    int size = Program.current_dir.Directory_table[index].size;
                    string content = "";
                    File_Entry f = new File_Entry(file_name.ToCharArray(), 0x0, fc, size, content, Program.current_dir);
                    f.Read_File();
                    content = f.content.TrimEnd(new char[] { '*' });
                    StreamWriter sw = new StreamWriter(destination + "\\" + file_name);
                    sw.Write(content);
                    sw.Flush();
                    sw.Close();
                    Console.WriteLine();
                }

                else
                    Console.WriteLine("\nThe system cannot find the path specified at your computer.\n");
            }

            else
                Console.WriteLine("\nThe system cannot find the file specified.\n");

        }//

        public static void rename(string old, string New)//
        {

            int index = Program.current_dir.Search(old);
            int index2 = Program.current_dir.Search(New);

            if (index != -1)
            {
                if (index2 == -1)
                {
                    Directory_Entry d = Program.current_dir.Directory_table[index];
                    d.name = New.ToCharArray();
                    Program.current_dir.Directory_table.RemoveAt(index);
                    Program.current_dir.Directory_table.Insert(index, d);
                    Program.current_dir.Write_Directory();

                    Console.WriteLine();

                }

                else
                    Console.WriteLine("\nA duplicate file name exists, or the file cannot be found.\n");

            }

            else
                Console.WriteLine("\nThe system cannot find the file specified.\n");
        }

        public static void del(string name)//
        {
            int index = Program.current_dir.Search(name);
            if (index != -1 && Program.current_dir.Directory_table[index].attribute == 0x0)
            {
                int fc = Program.current_dir.Directory_table[index].first_cluster;
                int size = Program.current_dir.Directory_table[index].size;
                File_Entry f = new File_Entry(name.ToCharArray(), 0x0, fc, size, "", Program.current_dir);
                f.Delete_File();
                Console.WriteLine();

            }
            else
                Console.WriteLine("\nThe system cannot find the file specified.\n");
        }

        public static void copy(string source, string dest)
        {
            var path = dest.Split('\\');
            int index = Program.current_dir.Search(source);
            int index2 = Program.current_dir.Search(path[0]);


            string fname = "";

            if (path.Length > 0)
                fname = path[path.Length - 1];

            if (index != -1 && index2 != -1 && Program.current_dir.Directory_table[index2].attribute == 0x10 && path.Length == 1)
            {
                Directory d = new Directory(dest.ToCharArray(), 0x10,
                  Program.current_dir.Directory_table[index2].first_cluster, Program.current_dir);
                d.Read_Directory();

                d.Directory_table.Add(Program.current_dir.Directory_table[index].Read_Directory_Entry());
                d.Write_Directory();

                if (d.Parent != null)
                {
                    d.Parent.Update_Content(d.Read_Directory_Entry());
                    d.Parent.Write_Directory();
                }

                Console.WriteLine("\n\t1 file<s> copied.\t\n");
            }

            else if (index != -1 && index2 != -1 && Program.current_dir.Directory_table[index2].attribute == 0x10 && fname.Length > 1)
            {
                int fc = Program.current_dir.Directory_table[index].first_cluster;
                int size = Program.current_dir.Directory_table[index].size;
                File_Entry f = new File_Entry(Program.current_dir.Directory_table[index].name, 0x0, fc, size, "", Program.current_dir);
                f.Read_File();
                string f_content = f.content;
                int f_sz = f.size;

                Directory d = new Directory(dest.ToCharArray(), 0x10,
                Program.current_dir.Directory_table[index2].first_cluster, Program.current_dir);
                d.Read_Directory();
                int index3 = d.Search(fname);
                if (index3 != -1)
                {
                    Console.Write($"\nOverwrite {fname} ? (Yes / No) :");
                    string x = Console.ReadLine().ToLower();
                    if (x == "y")
                    {
                        int fc2 = d.Directory_table[index3].first_cluster;

                        File_Entry f2 = new File_Entry(fname.ToCharArray(), 0x0, fc, f_sz, "", d);
                        f2.content = f_content;


                        f2.Write_File();

                        Directory_Entry df = new Directory_Entry(fname.ToCharArray(), 0x0, fc, f_sz);


                        d.Update_Content(df);
                        d.Write_Directory();


                        Console.WriteLine("\n\t1 file<s> copied.\t\n");
                    }

                    else if (x == "n")
                        Console.WriteLine("\n\t0 file<s> copied.\t\n");
                    else
                        Console.WriteLine("\nEntert y or n\n");
                }

                else
                    Console.WriteLine("\nThe system cannot find path specified.\n");
            }

            else
                Console.WriteLine("\nThe system cannot find path specified.\n");
        }

        public static void quit()
        {
            Environment.Exit(0);
        }//


        public static void cd2(string path)
        {
            string[] splitted = path.Split('\\');
            if (splitted.Length == 1)
            {
                Directory root = new Directory("C:".ToCharArray(), 0x10, 5);
                Program.current_dir = root;
                Program.current_dir.Read_Directory();
                Program.current_path = "C:";
                Commands.CurrentDirectory = "C:";
            }
            else if (splitted.Length == 2)
            {
                Directory root = new Directory("C:".ToCharArray(), 0x10, 5);
                Program.current_dir = root;
                Program.current_dir.Read_Directory();
                Program.current_path = "C:";
                Commands.CurrentDirectory = "C:";
                string dir = splitted[1];
                int index = Program.current_dir.Search(dir);

                if (index != -1 && Program.current_dir.Directory_table[index].attribute == 0x10)
                {
                    int fc = Program.current_dir.Directory_table[index].first_cluster;
                    Directory obj = new Directory(dir.ToCharArray(), 0x10, fc, Program.current_dir);
                    Program.current_dir = obj;
                    string str = new string(obj.name).Trim('\0');
                    Program.current_path = '\\' + str;
                    Program.current_dir.Read_Directory();
                    Commands.CurrentDirectory += Program.current_path;
                    Console.WriteLine();

                }
               
            }
            else if (splitted.Length > 2) 
            {
                Directory root = new Directory("C:".ToCharArray(), 0x10, 5);
                Program.current_dir = root;
                Program.current_dir.Read_Directory();
                Program.current_path = "C:";
                Commands.CurrentDirectory = "C:";
                string parent, sub;
                for (int i = 0; i < splitted.Length - 2; ++i)
                {
                     parent = splitted[i + 1];
                     sub = splitted[i + 2];
                    int index1 = Program.current_dir.Search(parent);
                    if (index1 != -1 && Program.current_dir.Directory_table[index1].attribute == 0x10)
                    {
                        int fc1 = Program.current_dir.Directory_table[index1].first_cluster;
                        Directory obj1 = new Directory(parent.ToCharArray(), 0x10, fc1, Program.current_dir);
                        Program.current_dir = obj1;
                        string zzz = new string(obj1.name).Trim('\0');
                        Program.current_path = '\\' + zzz;
                        Program.current_dir.Read_Directory();
                        Commands.CurrentDirectory += Program.current_path;
                        int index2 = Program.current_dir.Search(sub);

                        if (index2 != -1 && Program.current_dir.Directory_table[index2].attribute == 0x10)
                        {
                            int fc2 = Program.current_dir.Directory_table[index2].first_cluster;
                            Directory obj2 = new Directory(sub.ToCharArray(), 0x10, fc2, Program.current_dir);
                            Program.current_dir = obj2;
                            string xxx = new string(obj2.name).Trim('\0');
                            Program.current_path = '\\' + xxx;
                            Program.current_dir.Read_Directory();
                            Commands.CurrentDirectory += Program.current_path;
                            Console.WriteLine();


                        }
                        else
                            Console.WriteLine("The system cannot find the path specified");
                    }
                    else
                    {
                        Console.WriteLine("\nThe system cannot find the path specified.\n");
                    }
                }
            }
        }
    }

  public class Cmd
    {
        public static void Initialize()
        {
            Console.WriteLine("Microsoft Windows [Version 10.0.19044.1645]" +
                "\n(c) Microsoft Corporation. All rights reserved.\n");

            var comms = new[] { "cls", "help", "quit", "cd", "dir", "copy", "del", "md",
                                        "rd", "rename", "type", "import", "export" };
            bool flag = false;
            Commands.CurrentDirectory = "C:";

            
            while (true)
            {
                Console.Write(Commands.CurrentDirectory + ">");


                string line = Console.ReadLine();

                if (string.IsNullOrEmpty(line))                 
                    continue;
                

                char[] whitespace ={ ' ', '\t' };
                var splitted = line.Split(whitespace, StringSplitOptions.RemoveEmptyEntries);
             

                splitted[0] = splitted[0].ToLower();
                for (int i = 0; i < comms.Length; i++) 
                if (splitted[0] == comms[i])
                {
                    flag = true;
                    break;
                }

              

                if (flag && splitted[0] == "help")
                {
                    if (splitted.Length == 1)
                        Commands.help();
                    else if (splitted.Length == 2)
                    {
                        bool temp = false;
                        splitted[1] = splitted[1].ToLower();
                        foreach (var i in comms)
                        {
                            if (splitted[1] == i)
                            {
                                temp = true;
                                break;
                            }

                        }

                        if (temp)
                            Commands.help(splitted[1]);

                        else
                            Console.WriteLine($"This {splitted[1]} is not supported by the help utility.\n");


                    }
                    else
                        Console.WriteLine("\nThe syntax of the command is incorrect.\n");

                }

                

                else if (flag && splitted[0] == "del" )
                {
                    if (splitted.Length == 2)
                    {
                        Commands.del(splitted[1]);
                        Console.WriteLine();
                    }
                    else
                        Console.WriteLine("\nThe syntax of the command is incorrect.\n");

                }

                else if (flag && splitted[0] == "dir" )
                {
                    if (splitted.Length == 1)
                        Commands.dir();
                    else if (splitted.Length == 2)
                    {
                        Commands.dir();
                        Console.WriteLine();
                    }
                    else
                        Console.WriteLine("\nThe syntax of the command is incorrect.\n");

                }

                else if (flag  && splitted[0] == "md" )
                {
                    if (splitted.Length == 2)
                    {
                        Commands.md(splitted[1]);
                        Console.WriteLine();
                    }
                    else
                        Console.WriteLine("\nThe syntax of the command is incorrect.\n");

                }

                else if (splitted[0] == "cls" && flag )
                {
                    if (splitted.Length == 1)
                        Commands.cls();
                    else
                        Console.WriteLine("\nThe syntax of the command is incorrect.\n");
                    Console.Write("\n");
                }

               else if (splitted[0] == "quit" && flag)
                {
                    if (splitted.Length == 1)
                        Commands.quit();
                    else
                        Console.WriteLine("\nThe syntax of the command is incorrect.\n");

                }

                else if (flag && splitted[0] == "cd")
                {
                    if (splitted.Length == 1)
                        Console.WriteLine(Commands.CurrentDirectory + "\n");

                    else if (splitted.Length == 2)
                    {
                        if (splitted[1].Contains('\\'))
                            Commands.cd2(splitted[1]);
                        else
                            Commands.cd(splitted[1]);

                        Console.WriteLine();
                    }
                    else
                        Console.WriteLine("\nThe syntax of the command is incorrect.\n");
                }

                else if (flag && splitted[0] == "rd")
                {
                    if (splitted.Length == 2)
                    { 
                        Commands.rd(splitted[1]);
                        Console.WriteLine();
                    }
                    else
                        Console.WriteLine("\nThe syntax of the command is incorrect.\n");

                }

                else if (flag && splitted[0] == "type")
                {
                    if (splitted.Length == 2)
                    {
                        Commands.type(splitted[1]);
                        Console.WriteLine();

                    }
                    else
                        Console.WriteLine("\nThe syntax of the command is incorrect.\n");

                }

                else if (flag && splitted[0] == "copy") 
                {
                    if (splitted.Length == 3)
                    {
                        Commands.copy(splitted[1], splitted[2]);
                        Console.WriteLine();
                    }

                    else
                        Console.WriteLine("\nThe syntax of the command is incorrect.\n");

                }

                else if(flag && splitted[0] == "rename")
                {
                    if(splitted.Length==3)
                    {
                        Commands.rename(splitted[1], splitted[2]);
                        Console.WriteLine();
                    }
                    else
                        Console.WriteLine("\nThe syntax of the command is incorrect.\n");

                }

                else if (flag && splitted[0] == "import")
                {
                    if (splitted.Length == 2)
                    {
                        Commands.import(splitted[1]);
                        Console.WriteLine();
                    }
                    else
                        Console.WriteLine("\nThe syntax of the command is incorrect.\n");

                }

                else if(flag && splitted[0] == "export")
                {
                    if (splitted.Length == 3)
                    {
                        Commands.export(splitted[1], splitted[2]);
                        Console.WriteLine();
                    }
                    else
                        Console.WriteLine("\nThe syntax of the command is incorrect.\n");

                }

                if (flag == false) 
                Console.WriteLine("\'" + splitted[0] + "\' " +
                    "is not registerd as an internal or external command," +
                       "\noperable program or batch file.\n");
            }

        }
    }
    

    class Program
    {
        public static Directory current_dir;
        public static string current_path;

        static void Main(string[] args)
        {
            Virtual_Disk.Initialize();
            Cmd.Initialize();



        }
    }
}
