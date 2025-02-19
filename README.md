<div align="center">
<img alt="LOGO" src="https://github.com/SweetSmellFox/MFAWPF/blob/master/logo.png" width="256" height="256" />

# MFAWPF

[English](./README_en.md) | [简体中文](./README.md)

</div>

## 基本介绍

- 本项目是一个基于 WPF 框架开发的用户界面，旨在提供类似于 MaaPiCli 的功能

## 说明

### 使用需求

- .NET8 运行库
- 一个基于 maaframework 的非集成项目

### 如何使用

#### 自动安装

- 下载项目中 workflows/install.yml 并修改`项目名称`,`作者名`,`项目名`,`MAAxxx`
- 将修改后的 install.yml 替换 MAA 项目模板.github/workflows/install.yml
- 推送新版本

#### 手动安装

- 下载最新发行版并解压
- 将 maafw 项目中 assets/resource 中所有内容复制到 MFAWPF/Resource 中
- 将 maafw 项目中 assets/interface.json 文件复制到 MFAWPF/中
- **_修改_**刚刚复制的 interface.json 文件
- 下面是一个例子

```
{
 "resource": [
   {
     "name": "官服",
     "path": "{PROJECT_DIR}/resource/base"
   },
   {
     "name": "Bilibili服",
     "path": [
       "{PROJECT_DIR}/resource/base",
       "{PROJECT_DIR}/resource/bilibili"
     ]
   }
 ],
 "task": [
   {
     "name": "任务",
     "entry": "任务"
   }
 ]
}
```

修改为

```
{
  "name": "项目名称", //默认为null
  "version":  "项目版本", //默认为null
  "mirrorchyan_rid":  "项目ID(从Mirror酱下载的必要字段)", //默认为null , 比如 M9A
  "url":  "项目链接(目前应该只支持Github)", //默认为null , 比如 https://github.com/{Github账户}/{Github项目}
  "custom_title": "自定义标题", //默认为null, 使用该字段后，标题栏将只显示custom_title和version
  "default_controller": "adb", //默认为adb, 启动后的默认控制器，可选项 adb , win32
  "lock_controller":false, //默认为false, 是否锁定控制器，开启后用户不能在adb和win32中切换控制器
  "resource": [
    {
      "name": "官服",
      "path": "{PROJECT_DIR}/resource/base"
    },
    {
      "name": "Bilibili服",
      "path": [
        "{PROJECT_DIR}/resource/base",
        "{PROJECT_DIR}/resource/bilibili"
      ]
    }
  ],
  "task": [
    {
      "name": "任务",
      "entry": "任务接口",
      "check": true,  //默认为false，任务默认是否被选中
      "doc": "文档介绍",  //默认为null，显示在任务设置选项底下，可支持富文本，格式在下方
      "repeatable": true,  //默认为false，任务可不可以重复运行
      "repeat_count": 1,  //任务默认重复运行次数，需要repeatable为true
    }
  ]
}
```

### `doc`字符串格式：

#### 使用类似`[color:red]`文本内容`[/color]`的标记来定义文本样式。

#### 支持的标记包括：

- `[color:color_name]`：颜色，例如`[color:red]`。

- `[size:font_size]`：字号，例如`[size:20]`。

- `[b]`：粗体。

- `[i]`：斜体。

- `[u]`：下划线。

- `[s]`：删除线。

**注：上面注释内容为文档介绍用，实际运行时不建议写入。**

- 运行

## 开发相关

- 内置 MFATools 可以用来裁剪图片和获取 ROI
- 目前一些地方并没有特别完善,欢迎各位大佬贡献代码
- 注意，由于 `MaaFramework` 于 2.0 移除了 Exec Agent，所以目前无法通过注册 interface 注册 Custom Action 和 Custom Recognition
- `MFAWPF` 于 v1.2.3.3 加入动态注册 Custom Action 和 Custom Recognition 的功能，目前只支持 C#,需要在 Resource 目录的 custom 下放置相应的.cs 文件, 参考 [文档](./docs/zh_cn/自定义识别_操作.md)
- 在 exe 同级目录中放置 `logo.ico` 后可以替换窗口的图标
- `MFAWPF` 新增 interface 多语言支持,在`interface.json`同目录下新建`zh-cn.json`,`zh-tw.json`和`en-us.json`后，doc 和任务的 name 和选项的 name 可以使用 key 来指代。MFAWPF 会自动根据语言来读取文件的 key 对应的 value。如果没有则默认为 key

**注：在 MFA 中，于 Pipeline 中任务新增了俩个属性字段，分别为 `focus_tip` 和 `focus_tip_color`。**

- `focus` : _bool_  
  是否启用`focus_tip`。可选，默认 false。
- `focus_tip` : _string_ | _list<string, >_  
  当执行某任务时，在 MFA 右侧日志输出的内容。可选，默认空。
- `focus_tip_color` : _string_ | _list<string, >_  
  当执行某任务时，在 MFA 右侧日志输出的内容的颜色。可选，默认为 Gray。

## 致谢

### 开源库

- [MaaFramework](https://github.com/MaaAssistantArknights/MaaFramework)：自动化测试框架
- [MaaFramework.Binding.CSharp](https://github.com/MaaXYZ/MaaFramework.Binding.CSharp)：MaaFramework 的 C# 包装
- [HandyControls](https://github.com/ghost1372/HandyControls)：C# WPF 控件库
- [Serilog](https://github.com/serilog/serilog)：C# 日志记录库
- [Newtonsoft.Json](https://github.com/CommunityToolkit/dotnet)：C# JSON 库

## 画大饼

### v1.0

- [x] Pipeline 的 GUI 编辑界面
- [x] Support EN

### v1.2

- [ ] <strike>interface.json 的 GUI 编辑界面</strike>
