# csharp-Protoshift

<div align="center">
    <img alt="GitHub release (latest by date)" src="https://img.shields.io/github/v/release/YYHEggEgg/csharp-Protoshift?logo=csharp&style=for-the-badge"> 
    <img alt="GitHub" src="https://img.shields.io/github/license/YYHEggEgg/csharp-Protoshift?style=for-the-badge"> 
    <img alt="GitHub last commit" src="https://img.shields.io/github/last-commit/YYHEggEgg/csharp-Protoshift?style=for-the-badge"> 
    <img alt="GitHub Workflow Status" src="https://img.shields.io/github/actions/workflow/status/YYHEggEgg/csharp-Protoshift/pull-request-check.yml?branch=development&logo=github&style=for-the-badge">
</div>

<div align="center">
  <a href="https://discord.gg/NcAjuCSFvZ">
    <img alt="Discord - miHomo Software" src="https://img.shields.io/discord/1144970607616860171?label=Discord&logo=discord&style=for-the-badge">
  </a>
</div>

[EN](../README.md) | 中文

csharp-Protoshift 可为某二游提供先进、易管理的兼容性扩展。

**注意**: 我们始终欢迎项目的贡献者。但在做贡献之前，请仔细阅读我们的 [贡献规则](../CONTRIBUTING.md)。

## 目前支持的功能

- 基础 Protoshift 功能。其可以增强某二游服务器的兼容性。
- 代理服务器简单管理命令。
- Windy (lua) 脚本自动编译执行。下图显示的是在 `config_example.json` 中默认启用的 `welcome-to-csharp-Protoshift.lua` 配置，您可以通过将 `#/WindyConfig/OnlineExecWindys/[0]/OnlineExecMode` 设为 `Disabled` 来将其关闭。

  ![Windy Preview](../csharp-Protoshift/Images/windy_welcome-to-csharp-Protoshift.jpg)

- Protobuf / `query_cur_region` / Ec2b 等工具命令。

## 快速安装指南

### 前置环境需求

您首先需要确认您已经安装了以下软件：

- [.NET 6.0 Runtime](https://dotnet.microsoft.com/zh-cn/download/dotnet/6.0)。它用于生成与运行软件。
- [Git](https://git-scm.com/downloads)。它用于克隆源代码以及收取本软件的更新。
- [Protocol Buffers 编译器（protoc）](https://github.com/protocolbuffers/protobuf/releases/tag/v21.12)，并添加到您的系统的 PATH 环境变量。它用于实时编译必要文件。为保持一致性，我们建议使用版本 21.12。  
  **注意请根据您的系统下载发行页下方以 `protoc` 开头的压缩包**。此处同样提供部分下载链接：[v21.12 Windows x64](https://github.com/protocolbuffers/protobuf/releases/download/v21.12/protoc-21.12-win64.zip) | [v21.12 Linux x64](https://github.com/protocolbuffers/protobuf/releases/download/v21.12/protoc-21.12-linux-x86_64.zip)
- 一个真正的服务器。

除此之外，我个人强烈建议您：

- 在运行构建（`./update`）时保证对 GitHub 的**稳定**访问。
- 使用 VS Code 以获得路径快捷跳转、JSON Schema 等支持。

如果您由于某些原因无法将以上软件添加到系统环境变量，可以指示程序调用其绝对路径。有关进行这种特殊配置的细节，请参见 [Wiki - Building - 前置环境需求](https://github.com/YYHEggEgg/csharp-Protoshift/wiki/CN_Building#前置环境需求) 指南。

### 构建并运行

确认完成了前置条件后，只需要运行以下命令即可：

```sh
git clone --branch main https://github.com/YYHEggEgg/csharp-Protoshift
cd csharp-Protoshift
./update
```

完成后，您还需要进行一些配置。打开 [csharp-Protoshift/config.json](../csharp-Protoshift/config.json)，便可以看到注释行 _（注意这个文件只有构建完毕才有）_：

```json
{
// ...
  "NetConfig": {
    "BindAddress": "0.0.0.0",
    "BindPort": 22102,
    // Set your real server ip here
    // "RemoteAddress": {
    //   "IpAddress": "127.0.0.1",
    //   "AddressPort": 20041
    // }
  },
// ...
}
```

取消注释并以您的真实服务器地址替换，就可以完成 `config.json` 的最小配置。配置完 `config.json` 后，应确认 `resources` 是否正确配置。有关如何对 `resources` 进行正确配置，请参阅 [Wiki - Resources](https://github.com/YYHEggEgg/csharp-Protoshift/wiki/CN_Resources).

最后运行以下命令即可立刻启动服务器：

```sh
./run
```

您以后也可以随时使用 `./run` 启动 Protoshift 服务器，使用 `./update` 来获取更新。当然如果您想要以 Release 运行（可达到更高的性能），可以使用 `./scripts/run-rel`.

## 更多用法

- 有关导出稳定的发布版本，您可以参见 [Wiki - Building - 导出单个发布版本](https://github.com/YYHEggEgg/csharp-Protoshift/wiki/CN_Building#导出单个发布版本) 指南。
- 有关指令更具体的用法，您可以参见 [Wiki - Commands](https://github.com/YYHEggEgg/csharp-Protoshift/wiki/CN_Commands) 指南。
- 构建时使用 [默认行为文件](../HandlerGenerator/Gencode_Configuration/default_protobuf_branches.txt) 中定义的 Proto 分支。有关使用其他 Proto 分支或更换仓库源，您可以参见 [Wiki - Building - Proto 远程抓取管理](https://github.com/YYHEggEgg/csharp-Protoshift/wiki/CN_Building#proto-远程抓取管理) 指南。
