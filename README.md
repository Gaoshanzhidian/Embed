## 基本说明
由于此前在学习嵌入式的时候，用keil的编程体验实在是太难受了，所以写了一个软件用来配合vscode写代码。
至于为什么不写一个插件，是因为我不会。
这个程序可以协助你创建一个简单的嵌入式项目框架。
为什么是“简单的”？
因为现在只支持stm32f10系列的标准外设库。
其他的懒得搞了，其实现在大框架做出来了，主要是要探测st提供的各种开发库的结构。
我的想法是写一个脚本来处理，还有包括其他的依赖也是一样，但是现在要准备期末考试，没时间写，等到暑假再说吧。

## 外部依赖
需要cmake，nija，stlink，arm-none-eabi-gcc。
如果是自己下载源代码编译，需要注意cmake，ninja.exe，stlink是放在程序同一文件夹下。
arm-none-eabi-gcc是可以在程序里面配置的，无所谓放在哪里。

## 编译说明
程序是在dotnet框架下写的。所以大家编译很简单，先下载dotnet9.0，然后直接dotnet build就可以了。

## 代码说明
基本上所有的东西都写在program.cs里面，macro.cs是用来放arm-none-eabi-gcc的宏定义的，因为实在太长了，所以抽离出来。
所有命令集中在Command结构体里面，对应的行为在CommandAction结构体里面，然后主函数里通过字典派发。
中间还有一层CommandHandler是用来处理错误的，但是这部分没写完，大概用用吧。
Utils类存放所有的工具函数，剩下的都是些枚举集合啥的。

## 使用说明
下载zip到本地，zip中已经包含了cmake，nija，stlink，由于arm-none-eabi-gcc实在比较大，所以需要自己下载了。
下载后需要注意，进入到stlink文件夹下，里面有个Program Files（x86）文件夹，里面还有一个stlink文件夹，把这个文件夹复制到C:\Program Files（x86）下面，否则烧录时会出问题。
需要帮助请使用embed help，或者直接embed也会打印帮助
首次使用请使用embed config配置环境，主要是你使用的标准外设库位置，vscode位置，和arm-none-eabi-gcc位置。
这里config会要求你输入hal库位置，由于现在还不支持，所以随便填没关系。
另外就是项目创建路径。这个的意思是当你创建一个项目时，会在这个项目目录下创建一个与项目同名的文件夹。
一个典型的使用方式如下：
embed std projectname projectpath  创建项目。如果不填projectpath就是使用config命令写入的项目创建路径。
embed build  编译文件。
embed load  烧录文件。

## 免责申明
这只是一个玩具项目，我感觉很不稳定。虽然我自己用着写了一个简单项目测试了一下没啥问题。但是随时可能出现莫名其妙的bug，大家自己注意一下。
