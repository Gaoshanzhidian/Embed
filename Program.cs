using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Security.Cryptography.X509Certificates;
using System.Diagnostics;
using System.Timers;
using System.Reflection;
using System.Numerics;
using System.Globalization;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.IO.Pipes;
using System.Text;
using System.Diagnostics.SymbolStore;

//枚举
enum Error
{
    Correct,
    Path_Not_Exists,
    Project_Not_Exists,
    Invalid_Argument,
    Few_Argument,
    Cmake_Failed,
    Ninja_Failed,
    No_Administor_Privilege,
}

enum Modify
{
    isSet,
    isAdd,
    isDelete,
    isClear,
}

readonly record struct Command(string Name)
{
    public static readonly Command Config = new("config");
    public static readonly Command Std = new("std");
    public static readonly Command Hal = new("hal");
    public static readonly Command Help = new("help");
    public static readonly Command Build = new("build");
    public static readonly Command Show = new("show");
    public static readonly Command Add = new("add");
    public static readonly Command Delete = new("delete");
    public static readonly Command Del = new("del");
    public static readonly Command Clear = new("clear");
    public static readonly Command Set = new("set");
    public static readonly Command Download = new("download");
    public static readonly Command Load = new("load");
    public override string ToString() => Name;
}

delegate Error CA(string[] args);

readonly record struct CommandAction(CA Function)
{
    public static readonly CommandAction Config = new(delegate (string[] args)
    {
        ProgramConfig config;
        if (args.Length == 6)
        {
            config = new ProgramConfig
            {
                Std = args[1],
                Hal = args[2],
                ProjectPath = args[3],
                EditorPath = args[4],
                CompilerPath=args[5],
            };

        }
        else if (args.Length == 2 && args[1] == ARGUMENT.DEFAULT)
        {
            config = ProgramConfig.Default();
        }
        else
        {
            string std = Utils.PromptUntilValid("标准外设库路径");
            string hal = Utils.PromptUntilValid("硬件抽象层库路径");
            string proj = Utils.PromptUntilValid("项目创建路径");
            string vs = Utils.PromptUntilValid("Vscode路径");
            string com = Utils.PromptUntilValid("编译器路径");
            config = new ProgramConfig { Std = std, Hal = hal, ProjectPath = proj, EditorPath = vs ,CompilerPath=com};
        }
        Program.ProgramConfigManager.Write(config);
        if (!Directory.Exists(FILE_NAME.StFlash_Config_Chiips))
        {
            // var code=Utils.RunAsAdministrator();
            // if (code == Error.No_Administor_Privilege) return code;
            var info = Directory.CreateSymbolicLink(FILE_NAME.StFlash_Config_Chiips, Path.Combine(Program.ExePath, "stlink\\Program Files (x86)\\stlink"));
            Utils.Prompt(info.Exists.ToString());
        }

        Utils.Prompt(@$"配置完成：
标准外设库路径：{config.Std}
硬件抽象层库路径：{config.Hal}
项目创建路径：{config.ProjectPath}
Vscode路径{config.EditorPath}
编译器路径{config.CompilerPath}"
                    );
        return Error.Correct;
    });

    public static readonly CommandAction Std = new(delegate (string[] args)
    {
        // 从用户获取项目配置
        ProjectConfig config = Utils.Init(args);
        // 创建项目结构
        string project_name = config.ProjectName;
        string project_path = config.ProjectPath;
        string real_path = Path.Combine(project_path, project_name);
        string vscodeFolder = Path.Combine(real_path, FILE_NAME.ProjectPluginConfigDir);
        Directory.CreateDirectory(real_path);
        foreach (string src in config.SourceDirs)
        {
            Directory.CreateDirectory(Path.Combine(real_path, src));
        }
        foreach (string asm in config.AssembleDirs)
        {
            Directory.CreateDirectory(Path.Combine(real_path, asm));
        }
        foreach (string inc in config.IncludeDirs)
        {
            Directory.CreateDirectory(Path.Combine(real_path, inc));

        }
        Directory.CreateDirectory(Path.Combine(real_path, config.IntermediateOutput));
        Directory.CreateDirectory(Path.Combine(real_path, config.FinalOutput));
        Directory.CreateDirectory(vscodeFolder);
        // 附加std库文件以及目录
        Console.WriteLine("附加std文件");
        var std_path = Program.ProgramConfig.Std;
        var project_std_path = Path.Combine(real_path, "STD");
        var project_std_inc = Path.Combine(project_std_path, FILE_NAME.DefaultIncludeDirectory);
        var project_std_src = Path.Combine(project_std_path, FILE_NAME.DefaultSourceDirectory);
        var project_std_asm = Path.Combine(project_std_path, FILE_NAME.DefaultAssembleDirectory);
        Directory.CreateDirectory(project_std_path);
        Directory.CreateDirectory(project_std_inc);
        Directory.CreateDirectory(project_std_src);
        Directory.CreateDirectory(project_std_asm);
        foreach (string file in FileOrDirectoryList.StdIncFiles)
        {
            File.CreateSymbolicLink(Path.Combine(project_std_inc, Path.GetFileName(file)), Path.Combine(std_path, file));
            Console.WriteLine($"创建符号链接{Path.Combine(project_std_inc, Path.GetFileName(file))}==>{Path.Combine(std_path, file)}");
        }
        config.IncludeDirs.Add(project_std_inc);
        foreach (string dir in FileOrDirectoryList.StdIncDirs)
        {
            config.IncludeDirs.Add(Path.Combine(std_path,dir));
        }
        foreach (string file in FileOrDirectoryList.StdSrcFiles)
        {
            File.CreateSymbolicLink(Path.Combine(project_std_src, Path.GetFileName(file)), Path.Combine(std_path, file));
            Console.WriteLine($"创建符号链接{Path.Combine(project_std_src, Path.GetFileName(file))}==>{Path.Combine(std_path, file)}");
        }
        config.SourceDirs.Add(project_std_src);
        foreach (string dir in FileOrDirectoryList.StdSrcDirs)
        {
            Directory.CreateSymbolicLink(Path.Combine(project_std_src,Path.GetFileName(dir)), Path.Combine(std_path, dir));
            config.SourceDirs.Add(Path.Combine(std_path, dir));
            Console.WriteLine($"创建符号链接{Path.Combine(project_std_src,Path.GetFileName(dir))}==>{Path.Combine(std_path, dir)}");

        }
        foreach (string file in FileOrDirectoryList.StdAsmFiles)
        {
            File.CreateSymbolicLink(Path.Combine(project_std_asm,Path.GetFileName(file)), Path.Combine(std_path, file));
            Console.WriteLine($"创建符号链接{Path.Combine(project_std_asm,Path.GetFileName(file))}==>{Path.Combine(std_path, file)}");

        }
        config.AssembleDirs.Add(project_std_asm);
        foreach (string dir in FileOrDirectoryList.StdAsmDirs)
        {
            Directory.CreateSymbolicLink(Path.Combine(project_std_asm,Path.GetFileName(dir)), Path.Combine(std_path, dir));
            config.AssembleDirs.Add(Path.Combine(std_path, dir));
            Console.WriteLine($"创建符号链接{Path.Combine(project_std_asm,Path.GetFileName(dir))}==>{Path.Combine(std_path, dir)}");
        }
        // 附加std宏定义
        config.Define.AddRange(MacroDifineList.StdDefines);
        // 附加标准库头文件
        foreach (string dir in FileOrDirectoryList.ArmIncDirs)
        {
            config.IncludeDirs.Add(Path.Combine(Program.ProgramConfig.CompilerPath, dir));
        }
        // 写入项目配置
        Program.ProjectConfigManager.Write(config,Path.Combine(vscodeFolder, FILE_NAME.ProjectConfigFileName));
        string json = Utils.GeneratePropertiesJson(config);
        File.WriteAllText(Path.Combine(vscodeFolder, FILE_NAME.ProjectPluginConfigFileName), json);
        // File.WriteAllText(Path.Combine(vscodeFolder, FILE_NAME.ProjectConfigFileName), Program.ProjectConfigManager.TransformConfigToJson(config));
        // 启动vscode
        var psi = new ProcessStartInfo
        {
            FileName = Program.ProgramConfig.EditorPath,
            Arguments = $"\"{real_path}\"",
            UseShellExecute = false,
            CreateNoWindow = true
        };
        Process.Start(psi);
        return Error.Correct;
    });

    public static readonly CommandAction Hal = new(delegate (string[] args)
    {
        return Error.Correct;
    });

    public static readonly CommandAction Help = new(delegate (string[] args)
    {
        Utils.Prompt(
@"这是一个简单的嵌入式工程模板生成器。
基于stm32芯片。
你可以使用以下命令。
在命令行单指令模式下，
help 获取帮助，打印本段话。
config 配置标准外设库与硬件抽象层库的路径。第一个参数被识别为标准外设库路径，第二个参数被识别为硬件抽象层库路径。
default 使用默认的库。
……
");
        return Error.Correct;
    });

    public static readonly CommandAction Build = new(delegate (string[] args)
    {
        var proj_config_path = Utils.GetProjectConfigPathOrPrompt();
        if (proj_config_path is null) return Error.Project_Not_Exists;
        var config = Program.ProjectConfigManager.Read(proj_config_path);
        var proj_path = Path.Combine(config.ProjectPath, config.ProjectName);
        var cmakelist = Utils.GenerateCMakeListText(config);
        var linkscript = Utils.GenerateLinkScript();
        var cmakelist_path = Path.Combine(proj_path, FILE_NAME.CMakeList);
        var cmake_toolchain = Utils.GenerateCMakeToolchainText();
        var cmake_toolchain_path = Path.Combine(proj_path, FILE_NAME.CMakeToolchain);
        var linkscript_path = Path.Combine(proj_path,FILE_NAME.LinkScript);
        File.WriteAllText(cmakelist_path, cmakelist);
        File.WriteAllText(cmake_toolchain_path,cmake_toolchain);
        File.WriteAllText(linkscript_path,linkscript);
        var psi_cmake = new ProcessStartInfo
        {
            FileName = Path.Combine(Program.ExeDir, FILE_NAME.CMakePath),
            Arguments = $"-S {proj_path} -B {Path.Combine(proj_path, config.IntermediateOutput)} -G Ninja -DCMAKE_BUILD_TYPE=Release -DCMAKE_TOOLCHAIN_FILE={Path.Combine(proj_path,FILE_NAME.CMakeToolchain)}",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        var cmake_process=Process.Start(psi_cmake)!;
        Utils.CaptureErrorOrOutput(cmake_process);
        cmake_process.WaitForExit();
        if (cmake_process.ExitCode != 0) return Error.Cmake_Failed;
        cmake_process.Close();
        var psi_ninja = new ProcessStartInfo
        {
            FileName = Path.Combine(Program.ExeDir, FILE_NAME.NinjaPath),
            Arguments = $"-C {Path.Combine(proj_path, config.IntermediateOutput)}",//-f {Path.Combine(proj_path, config.IntermediateOutput,FILE_NAME.NinjaBuildFile)}",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        var ninja_process=Process.Start(psi_ninja)!;
        Utils.CaptureErrorOrOutput(ninja_process);
        ninja_process.WaitForExit();
        if (ninja_process.ExitCode != 0)
        {
            return Error.Ninja_Failed;
        }
        ninja_process.Close();
        return Error.Correct;
    });
    public static readonly CommandAction Download = new(delegate (string[] args)
    {
        var proj_config_path = Utils.GetProjectConfigPathOrPrompt();
        if (proj_config_path is null) return Error.Project_Not_Exists;
        var config = Program.ProjectConfigManager.Read(proj_config_path);
        var proj_path = Path.Combine(config.ProjectPath, config.ProjectName);
        var psi = new ProcessStartInfo
        {
            FileName = Path.Combine(Program.ExeDir, FILE_NAME.StFlashPath),
            Arguments = $"write {Path.Combine(proj_path,config.IntermediateOutput,config.ProjectName+".bin")} 0x08000000",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,

        };
        // psi.Environment["STLINK_CHIPDB_PATH"] = Path.Combine(Program.ExeDir,"stlink","chips");
        var process=Process.Start(psi)!;
        Utils.CaptureErrorOrOutput(process);
        process.WaitForExit();
        return Error.Correct;
    });
    public static readonly CommandAction Load = Download;
    public static readonly CommandAction Show = new(delegate (string[] args)
    {
        if (args.Length == 1)
        {
            var proj_config_path = Utils.GetProjectConfigPathOrPrompt();
            if (Path.Exists(proj_config_path))
            {
                Utils.Prompt(Program.ProjectConfigManager.ReadString(proj_config_path));
            }
            else
            {
                Utils.Prompt(Program.ProgramConfigManager.ReadString());
            }
        }
        else if (args.Length == 2)
        {
            switch (args[1])
            {
                case ARGUMENT.PROJECT:
                    var proj_config_path = Path.Combine(Program.CurrentDirectory, FILE_NAME.ProjectPluginConfigDir, FILE_NAME.ProjectConfigFileName);
                    Utils.Prompt(Program.ProjectConfigManager.ReadString(proj_config_path));
                    break;
                case ARGUMENT.PROGRAM:
                    Utils.Prompt(Program.ProgramConfigManager.ReadString());
                    break;
                default:
                    return Error.Invalid_Argument;
            }
        }
        return Error.Correct;
    });

    public static readonly CommandAction Add = new(delegate (string[] args)
    {
        if (args.Length >= 3)
        {
            var path = Utils.GetProjectConfigPathOrPrompt();
            if (path is null) return Error.Path_Not_Exists;

            var cmd = args[1];
            var values = args[2..];
            var config = Program.ProjectConfigManager.Read(path);

            var code = Utils.ModifyConfigList(config, cmd, values, Modify.isAdd);
            if (code == Error.Invalid_Argument)
            {
                return Error.Invalid_Argument;
            }
            Program.ProjectConfigManager.Write(config, path);
            Utils.Prompt(PROMT.Have_Executed + $"\n已添加 {string.Join(", ", values)} 至 {cmd}");
            return Error.Correct;
        }
        return Error.Few_Argument;
    });

    public static readonly CommandAction Delete = new(delegate (string[] args)
    {
        if (args.Length >= 3)
        {
            var path = Utils.GetProjectConfigPathOrPrompt();
            if (path is null) return Error.Path_Not_Exists;

            var cmd = args[1];
            var values = args[2..];
            var config = Program.ProjectConfigManager.Read(path);

            var code = Utils.ModifyConfigList(config, cmd, values, Modify.isDelete);
            if (code == Error.Invalid_Argument)
            {
                return Error.Invalid_Argument;
            }

            Program.ProjectConfigManager.Write(config, path);
            Utils.Prompt(PROMT.Have_Executed + $"\n已从 {cmd} 中删除 {string.Join(", ", values)}");
            return Error.Correct;
        }
        return Error.Few_Argument;
    });

    public static readonly CommandAction Del = Delete;

    public static readonly CommandAction Clear = new(delegate (string[] args)
    {
        if (args.Length >= 2)
        {
            var path = Utils.GetProjectConfigPathOrPrompt();
            if (path is null) return Error.Path_Not_Exists;
            var cmd = args[1];
            var values = args[2..];
            var config = Program.ProjectConfigManager.Read(path);
            var code = Utils.ModifyConfigList(config, cmd, values, Modify.isClear);
            var proj_path = Path.Combine(config.ProjectPath, config.ProjectName);
            if (code == Error.Invalid_Argument)
            {
                if (cmd == ARGUMENT.PROJECT)
                {
                    Directory.Delete(Path.Combine(proj_path, config.IntermediateOutput), true);
                    Directory.Delete(Path.Combine(proj_path, config.FinalOutput), true);
                    Directory.CreateDirectory(Path.Combine(proj_path, config.IntermediateOutput));
                    Directory.CreateDirectory(Path.Combine(proj_path, config.FinalOutput));
                    File.Delete(Path.Combine(proj_path, FILE_NAME.CMakeList));
                    File.Delete(Path.Combine(proj_path, FILE_NAME.CMakeToolchain));
                    File.Delete(Path.Combine(proj_path, "link.ld"));
                    return Error.Correct;
                }
                else
                {
                    return code;
                }
            }
            else
            {
                return code;
            }
        }
        return Error.Few_Argument;
    });

    public static readonly CommandAction Set = new(delegate (string[] args)
    {
        if (args.Length >= 3)
        {
            var path = Utils.GetProjectConfigPathOrPrompt();
            if (path is null) return Error.Path_Not_Exists;

            var cmd = args[1];
            var values = args[2..];
            var config = Program.ProjectConfigManager.Read(path);

            var code = Utils.ModifyConfigList(config, cmd, values, Modify.isSet);
            if (code == Error.Invalid_Argument)
            {
                return Error.Invalid_Argument;
            }
            Program.ProjectConfigManager.Write(config, path);
            Utils.Prompt(PROMT.Have_Executed + $"\n已将 {cmd} 设置为 {string.Join(", ", values)}");
            return Error.Correct;
        }
        return Error.Few_Argument;
    });
}

//字符串常量
class ARGUMENT
{
    public const string PROJECT = "project";
    public const string PROGRAM = "program";
    public const string DEFAULT = "default";
    public const string INCLUDEDIRECTORY = "IncludeDirectory";
    public const string INCDIR = "incdir";
    public const string SOURCEDIRECTORY = "SourceDirectory";
    public const string SRCDIR = "srcdir";
    public const string ASSEMBLEDIRECTORY = "AssembleDirectory";
    public const string ASMDIR = "asmdir";
    public const string DIFIEN = "define";
    public const string PROJECTNAME = "ProjectName";
    public const string NAME = "name";
    public const string PROJECTPATH = "ProjectPath";
    public const string Path = "path";
    public const string INTERMEDIATEOUTPUT = "IntermediateOutput";
    public const string INTEROUT = "interout";
    public const string FINALOUTPUT = "FinalOutput";
    public const string BINOUT = "binout";
    public const string FINALOUT = "finalout";
}

class PROMT
{
    //一般信息
    public const string No_Could_Show_Information = "没有找到需要显示的信息";
    public const string No_Project_Exists = "当前文件夹下无项目存在";
    public const string Have_Executed = "命令已执行";

    //错误信息 
    public const string Correct = "命令已执行";
    public const string Path_Not_Exists = "路径不存在";
    public const string Invalid_Argument = "无效的参数";
    public const string Project_Not_Exists = "项目不存在";
    public const string Few_Argument = "过少的参数";
    public const string Cmake_Failed = "CMake执行时出错";
    public const string Ninja_Failed = "Ninja执行时出错";
    public const string No_Administor_Privilege = "没有管理员权限，这可能导致烧录时出现问题";
}

class FILE_NAME
{
    public const string ProgramConfigFileName = "ProgramConfig.json";
    public const string ProjectConfigFileName = "ProjectConfig.json";
    public const string ProjectPluginConfigFileName = "c_cpp_properties.json";
    public const string ProjectPluginConfigDir = ".vscode";
    public const string DefaultProjectName = "Anonymous";
    public const string DefaultIntermediateOutput = "IntermediateOutput";
    public const string DefaultFinalOutput = "FinalOutput";
    public const string DefaultCompilerPath = @"D:\keil\core\ARM\ARMGNU";
    public const string DefaultIncludeDirectory = "Include";
    public const string DefaultSourceDirectory = "Source";
    public const string DefaultAssembleDirectory = "Assemble";
    public const string DefaultEditorPath = "D:\\Microsoft VS Code\\Code.exe";
    public const string CMakeList = "CMakeLists.txt";
    public const string CompilerBinary = @"bin\arm-none-eabi-gcc.exe";
    public const string CMakePath = @"CMake\bin\cmake.exe";
    public const string NinjaPath = "ninja.exe";
    public const string StFlashPath = @"stlink\bin\st-flash.exe";
    public const string CMakeToolchain = "toolchain.cmake";
    public const string NinjaBuildFile = "build.ninja";
    public const string LinkScript = "link.ld";
    public const string StFlash_Config_Chiips = @"C:\Program Files (x86)\stlink";
}


// 所需文件、目录集合
class FileOrDirectoryList
{
    public static readonly List<string> StdIncFiles = [
        @"Project\STM32F10x_StdPeriph_Template\stm32f10x_conf.h",
        @"Libraries\CMSIS\CM3\DeviceSupport\ST\STM32F10x\system_stm32f10x.h",
        @"Libraries\CMSIS\CM3\DeviceSupport\ST\STM32F10x\stm32f10x.h",
        @"Libraries\CMSIS\CM3\CoreSupport\core_cm3.h",
    ];
    public static readonly List<string> StdIncDirs = [
        @"Libraries\STM32F10x_StdPeriph_Driver\inc",
    ];
    public static readonly List<string> StdSrcFiles = [
        @"Libraries\CMSIS\CM3\DeviceSupport\ST\STM32F10x\system_stm32f10x.c",
        @"Libraries\CMSIS\CM3\CoreSupport\core_cm3.c",
    ];
    public static readonly List<string> StdSrcDirs = [
        @"Libraries\STM32F10x_StdPeriph_Driver\src",
    ];
    public static readonly List<string> StdAsmFiles = [
    // @"Libraries\CMSIS\CM3\DeviceSupport\ST\STM32F10x\startup\arm\startup_stm32f10x_md.s"
        
        @"Libraries\CMSIS\CM3\DeviceSupport\ST\STM32F10x\startup\gcc_ride7\startup_stm32f10x_md.s"
    ];
    public static readonly List<string> StdAsmDirs = [
    ];
    public static readonly List<string> ArmIncDirs = [
        @"lib\gcc\arm-none-eabi\14.2.1\include",
        @"lib\gcc\arm-none-eabi\14.2.1\include-fixed",
        @"arm-none-eabi\include"
    ];
}

// 所需宏定义集合
class MacroDifineList
{
    public static readonly List<string> StdDefines = [
        "USE_STDPERIPH_DRIVER",
        "STM32F10X_MD",
    ];
}

//配置管理
class ConfigManager<T>(string path)
{
    private readonly string path = path;
    public void Write(T config, string? path = null)
    {
        path ??= this.path;
        string json = JsonSerializer.Serialize(config, Program.JsonOptions);
        File.WriteAllText(path, json);
    }
    public string TransformConfigToJson(T config, bool indentedOption = true, string? path = null)
    {
        path ??= this.path;
        return JsonSerializer.Serialize(config, Program.JsonOptions);
    }

    public T Read(string? path = null)
    {
        path ??= this.path;
        if (!File.Exists(path)) return Activator.CreateInstance<T>();
        string json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<T>(json)!;
    }

    public string ReadString(string? path = null)
    {
        path ??= this.path;
        if (!File.Exists(path)) return "";
        return File.ReadAllText(path);
    }
}

class ProgramConfig
{
    public required string Std { get; set; }
    public required string Hal { get; set; } 
    public required string ProjectPath { get; set; } 
    public required string EditorPath { get; set; } 
    public required string CompilerPath{ get; set; }

    public static ProgramConfig Default()
    {
        return new ProgramConfig
        {
            Std = Path.Combine(Program.ExeDir, "Pack\\STM32F10x_StdPeriph_Lib_V3.6.0"),
            Hal = Path.Combine(Program.ExeDir, "Pack\\STM32CubeF1"),
            ProjectPath = Path.Combine(Program.ExeDir, "Project"),
            EditorPath = FILE_NAME.DefaultEditorPath,
            CompilerPath=FILE_NAME.DefaultCompilerPath,
        };
    }
}

class ProjectConfig
{
    public required string ProjectName { get; set; }
    public required string ProjectPath{ get; set; }
    public required string IntermediateOutput { get; set; }
    public required string FinalOutput { get; set; }
    public required List<string> IncludeDirs { get; set; }
    public required List<string> Define { get; set; }
    public required List<string> SourceDirs { get; set; }
    public required List<string> AssembleDirs { get; set; }

    public static ProjectConfig Default()
    {
        return new ProjectConfig
        {
            ProjectName = FILE_NAME.DefaultProjectName,
            ProjectPath = Program.ProgramConfig.ProjectPath,
            IntermediateOutput = FILE_NAME.DefaultIntermediateOutput,
            FinalOutput = FILE_NAME.DefaultFinalOutput,
            IncludeDirs = [FILE_NAME.DefaultIncludeDirectory],
            SourceDirs = [FILE_NAME.DefaultSourceDirectory],
            AssembleDirs = [FILE_NAME.DefaultAssembleDirectory],
            Define = [],
        };
    }
}

//工具函数
class Utils
{
    public static string PromptUntilValid(string message, int? number = null)
    {
        string? input;
        while (true && number == null)
        {
            Console.Write($"{message}:");
            input = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(input)) return input.Trim();
        }
        Console.Write($"{message}:");
        input = Console.ReadLine();
        return input == null ? "" : input.Trim();
    }

    public static void Prompt(string message)
    {
        Console.Write(">>");
        Console.WriteLine(message);
        // Console.Write("\n");
        // Console.Write("<<");
    }

    public static ProjectConfig Init(string[] args)
    {
        string proj_path = Program.ProgramConfig.ProjectPath;
        string proj_name = "";
        List<string> incDir = [];
        List<string> srcDir = [];
        List<string> assDir = [];
        string output = "";
        string outbin = "";
        List<string> define = [];

        if (args.Length == 3)
        {
            proj_name = args[1];
            proj_path = Path.Combine(args[2], proj_name);
        }
        else if (args.Length == 2)
        {
            proj_name = args[1];
            proj_path = Path.Combine(proj_path, proj_name);
        }
        else if (args.Length == 1)
        {
            proj_name = PromptUntilValid("项目名称", 1);
            proj_path = PromptUntilValid("项目路径", 1);
            incDir = [.. PromptUntilValid("头文件路径", 1).Split()];
            srcDir = [.. PromptUntilValid("源文件路径", 1).Split()];
            assDir = [.. PromptUntilValid("汇编文件路径", 1).Split()];
            output = PromptUntilValid("编译中间结果输出路径", 1);
            outbin = PromptUntilValid("编译最终结果输出路径", 1);
            define = [.. PromptUntilValid("宏定义值", 1).Split()];
        }
        var config = ProjectConfig.Default();
        config.ProjectName = !String.IsNullOrWhiteSpace(proj_name) ? proj_name : config.ProjectName;
        config.ProjectPath = !String.IsNullOrWhiteSpace(proj_path) ? proj_path : config.ProjectPath;
        config.IncludeDirs = !String.IsNullOrWhiteSpace(incDir[0]) ? incDir : config.IncludeDirs;
        config.SourceDirs = !String.IsNullOrWhiteSpace(srcDir[0]) ? srcDir : config.SourceDirs;
        config.AssembleDirs = !String.IsNullOrWhiteSpace(assDir[0]) ? assDir : config.AssembleDirs;
        config.Define = !String.IsNullOrWhiteSpace(define[0]) ? define : config.Define;
        config.IntermediateOutput = !String.IsNullOrWhiteSpace(output) ? output : config.IntermediateOutput;
        config.FinalOutput = !String.IsNullOrWhiteSpace(outbin) ? outbin : config.FinalOutput;
        return config;
    }

    public static string? GetProjectConfigPathOrPrompt()
    {
        var path = Path.Combine(Program.CurrentDirectory, FILE_NAME.ProjectPluginConfigDir, FILE_NAME.ProjectConfigFileName);
        if (!Path.Exists(path))
        {
            // Utils.Prompt(PROMT.No_Project_Exists);
            return null;
        }
        return path;
    }

    private static void ModifyList(List<string> config, string cmd, string[] values, Modify modify)
    {
        switch (modify)
        {
            case Modify.isSet:
                config = [.. values];
                break;
            case Modify.isAdd:
                config.AddRange(values);
                break;
            case Modify.isDelete:
                config.RemoveAll(s => values.Contains(s));
                break;
            case Modify.isClear:
                config = [];
                break;
        }
    }

    public static Error ModifyConfigList(ProjectConfig config, string cmd, string[] values, Modify modify)
    {
        switch (cmd)
        {
            case ARGUMENT.INCLUDEDIRECTORY:
            case ARGUMENT.INCDIR:
                ModifyList(config.IncludeDirs, cmd, values, modify);
                break;
            case ARGUMENT.SOURCEDIRECTORY:
            case ARGUMENT.SRCDIR:
                ModifyList(config.SourceDirs, cmd, values, modify);
                break;
            case ARGUMENT.ASSEMBLEDIRECTORY:
            case ARGUMENT.ASMDIR:
                ModifyList(config.AssembleDirs, cmd, values, modify);
                break;
            case ARGUMENT.DIFIEN:
                ModifyList(config.Define, cmd, values, modify);
                break;
            case ARGUMENT.PROJECTNAME:
            case ARGUMENT.NAME:
                if (values.Length > 0) config.ProjectName = values[0];
                break;
            case ARGUMENT.PROJECTPATH:
            case ARGUMENT.Path:
                if (values.Length > 0) config.ProjectPath = values[0];
                break;
            case ARGUMENT.INTERMEDIATEOUTPUT:
            case ARGUMENT.INTEROUT:
                if (values.Length > 0) config.IntermediateOutput = values[0];
                break;
            case ARGUMENT.FINALOUTPUT:
            case ARGUMENT.BINOUT:
            case ARGUMENT.FINALOUT:
                if (values.Length > 0) config.FinalOutput = values[0];
                break;
            default:
                // Prompt(PROMT.Invalid_Argument);
                return Error.Invalid_Argument;
        }
        return Error.Correct;
    }

    public static void CopyDirectory(string srcdir, string tardir)
    {
        foreach (var dirPath in Directory.GetDirectories(srcdir, "*", SearchOption.AllDirectories))
            Directory.CreateDirectory(dirPath.Replace(srcdir, tardir));

        foreach (var newPath in Directory.GetFiles(srcdir, "*.*", SearchOption.AllDirectories))
            File.Copy(newPath, newPath.Replace(srcdir, tardir), true);
    }
    public static void CaptureErrorOrOutput(Process process)
    {
        process.OutputDataReceived += (s, ev) => { Console.WriteLine(ev.Data); };
        process.ErrorDataReceived += (s, ev) => { Console.WriteLine(ev.Data); };
        process.Exited += (s, ev) => { Console.WriteLine(ev.ToString()); };
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
    }
    public static void PromptErrorCode(Error code)
    {
        switch (code)
        {
            case Error.Correct:
                Prompt(PROMT.Correct);
                break;
            case Error.Path_Not_Exists:
                Prompt(PROMT.Path_Not_Exists);
                break;
            case Error.Project_Not_Exists:
                Prompt(PROMT.Project_Not_Exists);
                break;
            case Error.Invalid_Argument:
                Prompt(PROMT.Invalid_Argument);
                break;
            case Error.Few_Argument:
                Prompt(PROMT.Few_Argument);
                break;
            case Error.Cmake_Failed:
                Prompt(PROMT.Cmake_Failed);
                break;
            case Error.Ninja_Failed:
                Prompt(PROMT.Ninja_Failed);
                break;
            case Error.No_Administor_Privilege:
                Prompt(PROMT.No_Administor_Privilege);
                break;
            default:
                Prompt("未设定提示的错误");
                break;
        }

    }

    public static string GeneratePropertiesJson(ProjectConfig config)
    {
        config.Define.AddRange([.. Program.macros.Split("\n")]);
        var json = new
        {
            configurations = new[]
            {
                new
                {
                    name=config.ProjectName,
                    includePath = config.IncludeDirs,
                    defines =  config.Define,
                    cStandard = "c99",
                    cppStandard = "c++17",
                    intelliSenseMode = "gcc-arm"
                },
            },
            version = 4
        };
        return JsonSerializer.Serialize(json, Program.JsonOptions);
    }

    public static string GenerateCMakeListText(ProjectConfig config)
    {
        var sb = new StringBuilder();
        sb.AppendLine("cmake_minimum_required(VERSION 3.15)");
        sb.AppendLine($"project ({config.ProjectName} C)");
        sb.AppendLine("enable_language(ASM)");
        sb.AppendLine();

        sb.AppendLine("set(CMAKE_SYSTEM_NAME Gneric)");
        sb.AppendLine("set(CMAKE_SYSTEM_PROCESSOR ARM)");
        sb.AppendLine();

        sb.AppendLine("set(CMAKE_C_STANDARD 11)");
        sb.AppendLine("set(CMAKE_C_EXTENSIONS OFF)");
        sb.AppendLine();

        sb.AppendLine("set(CMAKE_C_FLAGS \"-mcpu=cortex-m3 -mthumb -ffunction-sections -fdata-sections -Wall\")");
        // sb.AppendLine("set(CMAKE_EXE_LINKER_FLAGS \"-Wl,--gc-sections\")");

        sb.AppendLine("include_directories(");
        foreach (string dir in config.IncludeDirs)
        {
            sb.AppendLine($"    \"{dir.Replace("\\", "/")}\"");
        }
        sb.AppendLine(")");
        sb.AppendLine();

        if (config.Define != null && config.Define.Count > 0)
        {
            sb.AppendLine("add_definitions(");
            foreach (string def in config.Define)
            {
                var defs = def.Trim().Split(' ');
                if (defs.Length == 1)
                {
                    sb.AppendLine($"    -D{def}");
                }
                else
                {
                    sb.AppendLine($"    -D{defs[0]} ={def[defs[0].Length..]}");
                }
            }
            sb.AppendLine(")");
            sb.AppendLine();
        }

        sb.AppendLine("file(GLOB_RECURSE SOURCES");
        foreach (string dir in config.SourceDirs)
        {
            sb.AppendLine($"    \"{dir.Replace("\\", "/")}/*.c\"");
        }
        sb.AppendLine(")");
        sb.AppendLine();

        sb.AppendLine("file(GLOB_RECURSE ASMS");
        foreach (string dir in config.AssembleDirs)
        {
            sb.AppendLine($"    \"{dir.Replace("\\", "/")}/*.s\"");
        }
        sb.AppendLine(")");
        sb.AppendLine("set_source_files_properties(${ASMS} PROPERTIES LANGUAGE ASM)");
        sb.AppendLine("list(APPEND SOURCES ${ASMS})");
        sb.AppendLine();

        sb.AppendLine($"set(LINKER_SCRIPT {Path.Combine(config.ProjectPath, config.ProjectName).Replace("\\", "/")}/link.ld)");
        sb.AppendLine("add_link_options(-Wl,--gc-sections)");
        sb.AppendLine("add_link_options(-T${LINKER_SCRIPT})");

        sb.AppendLine("add_executable(${PROJECT_NAME}.elf ${SOURCES})");
        sb.AppendLine(@"set(CMAKE_OBJCOPY arm-none-eabi-objcopy)
    add_custom_command(TARGET ${PROJECT_NAME}.elf POST_BUILD
    COMMAND ${CMAKE_OBJCOPY} -O ihex ${PROJECT_NAME}.elf ${PROJECT_NAME}.hex
    COMMAND ${CMAKE_OBJCOPY} -O binary ${PROJECT_NAME}.elf ${PROJECT_NAME}.bin
    COMMENT ""Generating HEX and BIN files"")
");
        return sb.ToString();
    }

    public static string GenerateCMakeToolchainText()
    {
        //toolchain.cmake    set(CMAKE_CXX_COMPILER ""D:/keil/core/ARM/ARMGNU/bin/arm-none-eabi-g++.exe"")
        return @$"
set(CMAKE_SYSTEM_NAME Generic)
set(CMAKE_SYSTEM_PROCESSOR arm)

set(CMAKE_C_COMPILER ""{Path.Combine(Program.ProgramConfig.CompilerPath, FILE_NAME.CompilerBinary).Replace("\\", "/")}"")

# 不要尝试运行可执行程序
set(CMAKE_TRY_COMPILE_TARGET_TYPE STATIC_LIBRARY)

# 可选：设置链接器、汇编器、工具链前缀等
#set(CMAKE_ASM_COMPILER arm-none-eabi-as)";
    }

    public static string GenerateLinkScript()
    {
        return @"/* Linker script for STM32F103C8T6 (64KB Flash, 20KB RAM) */

/* Entry Point */
ENTRY(Reset_Handler)

/* Memory Regions */
MEMORY
{
  FLASH (rx) : ORIGIN = 0x08000000, LENGTH = 64K
  RAM (xrw)  : ORIGIN = 0x20000000, LENGTH = 20K
}

/* Define symbols for stack and heap */
_estack = ORIGIN(RAM) + LENGTH(RAM); /* Stack top immediately after RAM */
_Min_Heap_Size = 0x200;  /* Minimum heap size (bytes) */
_Min_Stack_Size = 0x400; /* Minimum stack size (bytes) */

/* Sections */
SECTIONS
{
  /* The startup code goes first in FLASH */
  .isr_vector :
  {
    . = ALIGN(4);
    KEEP(*(.isr_vector)) /* Vector table and startup code */
    . = ALIGN(4);
  } >FLASH

  /* Program code and read-only data */
  .text :
  {
    . = ALIGN(4);
    *(.text)           /* .text sections (code) */
    *(.text*)          /* .text* sections (code) */
    *(.glue_7)         /* glue arm to thumb */
    *(.glue_7t)        /* glue thumb to arm */
    *(.eh_frame)

    KEEP (*(.init))    /* Initialization code */
    KEEP (*(.fini))    /* Termination code - Ensure this is placed before .data LMA */

    . = ALIGN(4);
    _etext = .;        /* End of code symbol */
  } >FLASH

  .rodata :
  {
    . = ALIGN(4);
    *(.rodata)         /* .rodata sections (constants, strings, etc.) */
    *(.rodata*)        /* .rodata* sections (constants, strings, etc.) */
    . = ALIGN(4);
  } >FLASH

  /* ARM exception handling tables */
  .ARM.extab : { . = ALIGN(4); *(.ARM.extab* .gnu.linkonce.armextab.*) . = ALIGN(4); } >FLASH
  .ARM.exidx : { . = ALIGN(4); *(.ARM.exidx* .gnu.linkonce.armexidx.*) . = ALIGN(4); } >FLASH

  /* Pre-init, init, and fini arrays (constructor/destructor functions)
     These must be loaded into FLASH. Their LMA must precede .data's LMA. */
  .preinit_array :
  {
    . = ALIGN(4);
    PROVIDE_HIDDEN (__preinit_array_start = .);
    KEEP (*(.preinit_array*))
    PROVIDE_HIDDEN (__preinit_array_end = .);
    . = ALIGN(4);
  } >FLASH

  .init_array :
  {
    . = ALIGN(4);
    PROVIDE_HIDDEN (__init_array_start = .);
    KEEP (*(SORT(.init_array.*)))
    KEEP (*(.init_array*))
    PROVIDE_HIDDEN (__init_array_end = .);
    . = ALIGN(4);
  } >FLASH

  .fini_array :
  {
    . = ALIGN(4);
    PROVIDE_HIDDEN (__fini_array_start = .);
    KEEP (*(SORT(.fini_array.*)))
    KEEP (*(.fini_array*))
    PROVIDE_HIDDEN (__fini_array_end = .);
    . = ALIGN(4);
  } >FLASH

  /* _sidata is the LMA (Load Address) for the .data section.
     It's crucial that this comes AFTER all other sections that are purely in FLASH. */
  _sidata = LOADADDR(.data);

  /* Initialized data section
     LMA (Load Address) is in FLASH, VMA (Virtual Address) is in RAM.
     Startup code must copy data from _sidata (FLASH) to _sdata (RAM). */
  .data :
  {
    . = ALIGN(4);
    _sdata = .;        /* Start of .data in RAM (VMA) */
    *(.data)           /* .data sections */
    *(.data*)          /* .data* sections */
    . = ALIGN(4);
    _edata = .;        /* End of .data in RAM (VMA) */
  } >RAM AT> FLASH     /* VMA is in RAM, LMA is in FLASH after previous FLASH sections */

  /* Uninitialized data section (BSS).
     This section occupies space in RAM but is not loaded from FLASH.
     Startup code must zero out this section from _sbss to _ebss. */
  .bss :
  {
    . = ALIGN(4);
    _sbss = .;         /* Start of .bss in RAM */
    *(.bss)
    *(.bss*)
    *(COMMON)
    . = ALIGN(4);
    _ebss = .;         /* End of .bss in RAM */
  } >RAM

  /* Heap section. Starts after .bss and grows upwards. */
  ._user_heap_stack : /* This combined section ensures stack is at the very end of RAM */
  {
    . = ALIGN(8);
    PROVIDE(end = .);  /* Standard symbol for end of .bss / start of heap, used by some C libraries */
    PROVIDE(_end = .); /* Another common symbol for the same purpose */
    . = . + _Min_Heap_Size;
    . = ALIGN(8);
    _sheap = .; /* Start of heap, can be used by custom malloc */
    . = . + _Min_Stack_Size; /* Reserve space for the stack at the end of this section */
    . = ALIGN(8);
    _eheap = ORIGIN(RAM) + LENGTH(RAM) - _Min_Stack_Size; /* End of heap if stack is placed above */
                                                          /* More traditionally, heap grows until stack */
  } >RAM

  /* .stack section explicitly (alternative to placing stack at end of ._user_heap_stack)
     If you use this, you might need to adjust _eheap calculation or how heap is managed.
     This example places the stack at the very end of RAM, growing downwards.
     The _estack defined earlier marks the absolute top. The startup code will set SP to _estack.
     The ._user_heap_stack section already reserves stack space, so this .stack definition might be redundant
     or an alternative way depending on how your startup code and heap are managed.
     For simplicity, usually, the stack pointer is just initialized to _estack, and heap uses memory up to
     _estack - _Min_Stack_Size.
  */
  /*
  .stack (NOLOAD):
  {
    . = ALIGN(8);
    _sstack = _estack - _Min_Stack_Size; // Start of stack space (bottom)
    . = _estack;                         // End of stack space (top)
  } >RAM
  */


  /* Check for memory overflows */
  /* Ensure the LMA of .data + its size does not exceed FLASH bounds */
  ASSERT( (_sidata + SIZEOF(.data)) <= (ORIGIN(FLASH) + LENGTH(FLASH)), ""Error: FLASH memory overflowed for .data LMA!"")
  /* Ensure the VMA of all RAM sections does not exceed RAM bounds */
  ASSERT( (_ebss + SIZEOF(._user_heap_stack)) <= (ORIGIN(RAM) + LENGTH(RAM)), ""Error: RAM memory overflowed!"")
  /* A simpler RAM check could be: ASSERT(_estack <= ORIGIN(RAM) + LENGTH(RAM), ""Error: RAM memory overflowed!""); */


  /* Remove unwanted sections from output file */
  /DISCARD/ :
  {
    libc.a ( * )
    libm.a ( * )
    libgcc.a ( * )
  }

  /* DWARF debug sections.
     Stabs debugging sections.  */
  .stab          0 : { *(.stab) }
  .stabstr       0 : { *(.stabstr) }
  .stab.excl     0 : { *(.stab.excl) }
  .stab.exclstr  0 : { *(.stab.exclstr) }
  .stab.index    0 : { *(.stab.index) }
  .stab.indexstr 0 : { *(.stab.indexstr) }
  .comment       0 : { *(.comment) }
  /* ARM attributes */
  .ARM.attributes 0 : { *(.ARM.attributes) }
}";
    }


    public static Error RunAsAdministrator()
    {
        // 重新启动应用程序以管理员身份
        var psi = new ProcessStartInfo
        {
            FileName = Program.ExeDir + "embed.exe",
            UseShellExecute = true,
            Verb = "runas"  // 请求管理员权限
        };

        try
        {
            Process.Start(psi);
        }
        catch
        {
            // 处理异常，例如用户拒绝提升权限
            return Error.No_Administor_Privilege;
        }
        return Error.Correct;
    }
}

class CommandHandler(CommandAction func)
{
    private readonly CommandAction func=func;
    public void Execute(string[] args)
    {
        var code = func.Function(args);
        Utils.PromptErrorCode(code);
    }
}

partial class Program
{
    public static readonly string ExePath = Assembly.GetEntryAssembly()?.Location ?? throw new Exception(Error.Path_Not_Exists.ToString());
    public static readonly string ExeDir = Path.GetDirectoryName(ExePath) ?? throw new Exception(Error.Path_Not_Exists.ToString());
    public static readonly string CurrentDirectory = Directory.GetCurrentDirectory();
    public static readonly ConfigManager<ProgramConfig> ProgramConfigManager = new(Path.Combine(ExeDir, FILE_NAME.ProgramConfigFileName));
    public static readonly ConfigManager<ProjectConfig> ProjectConfigManager = new (Path.Combine(CurrentDirectory, FILE_NAME.ProjectConfigFileName));
    public static readonly ProgramConfig ProgramConfig = ProgramConfigManager.Read();
    public static readonly JsonSerializerOptions JsonOptions = new () { WriteIndented = true };
    static readonly Dictionary<Command, CommandHandler> commands = new()
    {
        [Command.Config] = new CommandHandler(CommandAction.Config),
        [Command.Std] = new CommandHandler(CommandAction.Std),
        [Command.Hal] = new CommandHandler(CommandAction.Hal),
        [Command.Help] = new CommandHandler(CommandAction.Help),
        [Command.Build] = new CommandHandler(CommandAction.Build),
        [Command.Show] = new CommandHandler(CommandAction.Show),
        [Command.Add] = new CommandHandler(CommandAction.Add),
        [Command.Del] = new CommandHandler(CommandAction.Del),
        [Command.Delete] = new CommandHandler(CommandAction.Delete),
        [Command.Set] = new CommandHandler(CommandAction.Set),
        [Command.Clear] = new CommandHandler(CommandAction.Clear),
        [Command.Load] = new CommandHandler(CommandAction.Load),
        [Command.Download]=new CommandHandler(CommandAction.Download)
        // 其他命令类同样添加到此处
    };

    static void Main(string[] args)
    {
        if (args.Length > 0)
        {
            var cmd = new Command(args[0]);
            if (commands.TryGetValue(cmd, out var handler))
            {
                handler.Execute(args);
            }
        }
        else
        {
            CommandAction.Help.Function(args);
        }
    }
}