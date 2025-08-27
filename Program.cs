using System;
using System.IO;
using System.Linq;
using System.Diagnostics;

namespace PackFixer
{
    class Program
    {
        static void Main(string[] args)
        {
            // baseDir = 程序运行所在目录（替换工具根目录）/
            string baseDir = AppDomain.CurrentDomain.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);

            // 固定子目录
            string changeExe = Path.Combine(baseDir, "Changeexe");   // 存放待处理 exe 的目录
            string changeFile = Path.Combine(baseDir, "Changefile"); // 存放新版文件的目录
            string workDir = Path.Combine(baseDir, "work");          // 临时解压目录
            string srcDir = Path.Combine(baseDir, "src");            // 打包工具 src 目录
            string sevenZip = @"C:\Program Files\7-Zip\7z.exe";      // 7-Zip 程序路径（按需修改）

            // 新版文件路径（固定名字）
            string newNTADM = Path.Combine(changeFile, "NTADM002.exe");
            string newNTDOM = Path.Combine(changeFile, "NTDOM.dll");

            // 从 Changeexe\ 目录中找 exe（要求只有一个）
            var candidates = Directory.GetFiles(changeExe, "*.exe", SearchOption.TopDirectoryOnly);
            if (candidates.Length != 1)
            {
                Console.WriteLine("Changeexe 目录内需且仅需 1 个 exe");
                return;
            }

            // 目标 exe 的完整路径和名字
            string targetExe = candidates[0];
            string name = Path.GetFileNameWithoutExtension(targetExe); // 包名 = test

            // 1) 清理 work\ 目录
            if (Directory.Exists(workDir)) Directory.Delete(workDir, true);
            Directory.CreateDirectory(workDir);

            // 2) 解压到 work\ 下
            //   例：7z.exe x -y -o"D:\替换工具\work" "D:\替换工具\Changeexe\test.exe"
            //   解压结果 = work\test\…内容
            Run("cmd.exe", $"/c \"\"{sevenZip}\" x -y -o\"{workDir}\" \"{targetExe}\"\"", baseDir);

            // copyRoot = work\test
            string copyRoot = Path.Combine(workDir, name);

            // 如果解压出来没有 test\ 目录，而是直接文件，就让 copyRoot = work\
            if (!Directory.Exists(copyRoot))
            {
                copyRoot = workDir;
            }

            // 3) 替换 work\test\ 下的 NTADM002.exe 和 NTDOM.dll
            ReplaceAll(copyRoot, "NTADM002.exe", newNTADM);
            ReplaceAll(copyRoot, "NTDOM.dll", newNTDOM);

            // 4) 清空 src\，再把 work\test\ 的内容复制到 src\
            if (Directory.Exists(srcDir)) Directory.Delete(srcDir, true);
            Directory.CreateDirectory(srcDir);

            // 用 xcopy 把 copyRoot 下的所有东西（*）铺到 src\
            // /E：包含子目录 /I：目标当目录 /Y：覆盖不询问 /H：复制隐藏+系统文件
            Run("cmd.exe", $"/c xcopy \"{copyRoot}\\*\" \"{srcDir}\\\" /E /I /Y /H", baseDir);

            // 5) 调用打包工具：ch.bat <包名>
            Run("cmd.exe", $"/c ch.bat \"{name}\"", baseDir);

            Console.WriteLine($"{name} 完成");
        }

        // 在 root 目录树中递归查找 fileName，并用 newFile 覆盖（多处命中就全部覆盖）
        static void ReplaceAll(string root, string fileName, string newFile)
        {
            foreach (var f in Directory.GetFiles(root, fileName, SearchOption.AllDirectories))
            {
                File.Copy(newFile, f, true); // true = 覆盖
            }
        }

        // 执行外部命令（7z 解压、xcopy 复制、ch.bat 打包）
        static void Run(string exe, string args, string workDir)
        {
            var p = new Process();
            p.StartInfo.FileName = exe;             // 要运行的程序（cmd.exe）
            p.StartInfo.Arguments = args;           // 参数（例如 /c xcopy …）
            p.StartInfo.WorkingDirectory = workDir; // 当前目录
            p.StartInfo.UseShellExecute = false;
            p.Start();
            p.WaitForExit();                        // 等待执行完成
        }
    }
}
