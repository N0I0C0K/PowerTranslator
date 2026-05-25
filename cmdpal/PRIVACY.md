# Privacy Policy / 隐私政策

_Last updated / 最后更新: 2026-05-25_

[English](#english) | [中文](#中文)

---

## English

### Overview

**PowerTranslator** (the "Extension") is an open-source translation extension for the Windows [Command Palette](https://learn.microsoft.com/en-us/windows/powertoys/command-palette/overview). This document explains what data the Extension handles.

**Short version: The Extension does not collect, store, transmit, or share any personal information with the developer.** There is no analytics, no telemetry, no crash reporting, no user accounts, and no advertising.

### Information We Do Not Collect

The developer of the Extension does **not**:

- Collect personally identifiable information (name, email, IP address, device identifiers, etc.).
- Collect usage analytics or telemetry.
- Track your activity inside or outside the Extension.
- Operate any server that receives data from the Extension.
- Sell, rent, or share any data with advertisers or data brokers.

### Information Handled Locally on Your Device

The following data is processed **entirely on your own device** and never sent to the developer:

- **Text you submit for translation** — held in memory only for the duration of the request.
- **Settings and API keys** — stored by the Windows Command Palette settings system on your local machine. The Extension reads them at runtime but does not transmit them anywhere except to the third-party translation service you have explicitly selected and configured.
- **Clipboard content** — accessed only when you explicitly invoke a clipboard-based translation command. It is not logged, copied, or retained by the Extension.

### Third-Party Translation Services

To perform translation, the Extension sends the text you choose to translate to a third-party translation API of your choice. Currently supported providers include:

- **Youdao Translate** ([privacy policy](https://c.youdao.com/dict/law/youdao_dict_privacy.html))
- **DeepL** ([privacy policy](https://www.deepl.com/privacy/))

When you use these services:

- The text you submit for translation is sent over HTTPS directly from your device to the selected provider.
- If the provider requires an API key, that key is sent with the request as required by the provider's API.
- The handling of that data is governed by the provider's own privacy policy and terms of service, not by this Extension.

You can change or remove the selected provider at any time in the Extension's settings. If no provider is configured, no network requests are made.

### Permissions

The Extension declares the `internetClient` capability solely to communicate with the third-party translation services described above. It does not access any other network resources.

### Children's Privacy

The Extension is not directed to children under 13 and does not knowingly collect any information from them.

### Changes to This Policy

This privacy policy may be updated from time to time. Material changes will be reflected in the file in the project's GitHub repository, and the "Last updated" date at the top will be revised accordingly.

### Contact

Source code and issue tracker: <https://github.com/N0I0C0K/PowerTranslator>

For privacy-related questions, please open an issue in the repository above.

---

## 中文

### 概述

**PowerTranslator**（以下简称"本扩展"）是一款用于 Windows [命令面板（Command Palette）](https://learn.microsoft.com/zh-cn/windows/powertoys/command-palette/overview) 的开源翻译扩展。本文档说明本扩展如何处理数据。

**简短版本：本扩展不向开发者收集、存储、传输或共享任何个人信息。** 不包含任何分析、遥测、崩溃上报、用户账户或广告。

### 我们不收集的信息

本扩展开发者**不会**：

- 收集任何个人身份信息（姓名、邮箱、IP 地址、设备标识符等）。
- 收集使用分析数据或遥测数据。
- 跟踪您在扩展内外的任何活动。
- 运营任何接收来自本扩展数据的服务器。
- 向广告商或数据经纪商出售、出租或共享任何数据。

### 仅在本地设备处理的信息

以下数据**完全在您自己的设备上处理**，绝不会发送给开发者：

- **您提交的待翻译文本** —— 仅在请求期间保留在内存中。
- **设置项与 API 密钥** —— 由 Windows 命令面板的设置系统存储在您本机。本扩展仅在运行时读取，除发送给您显式选择并配置的第三方翻译服务外，不会传输到任何其他位置。
- **剪贴板内容** —— 仅在您显式触发剪贴板翻译命令时才会访问。本扩展不会记录、复制或保留剪贴板内容。

### 第三方翻译服务

为执行翻译，本扩展会将您选择翻译的文本发送至您指定的第三方翻译 API。目前支持的服务提供商包括：

- **有道翻译**（[隐私政策](https://ai.youdao.com/DOCSIRMA/html/trans/price/wbfy/index.html)）
- **DeepL**（[隐私政策](https://www.deepl.com/zh/privacy/)）

当您使用上述服务时：

- 您提交的待翻译文本通过 HTTPS 由您的设备直接发送至所选服务提供商。
- 如果该服务需要 API 密钥，密钥将依据该服务的 API 要求随请求一同发送。
- 上述数据的处理受所选服务提供商自身的隐私政策与服务条款约束，与本扩展无关。

您可以随时在本扩展的设置中更改或移除所选的服务提供商。若未配置任何服务提供商，本扩展不会发起任何网络请求。

### 权限说明

本扩展声明了 `internetClient` 权限，**仅**用于与上述第三方翻译服务通信，不会访问任何其他网络资源。

### 儿童隐私

本扩展并非面向 13 岁以下儿童设计，也不会有意收集任何来自儿童的信息。

### 政策变更

本隐私政策可能不定期更新。重大变更会反映在项目 GitHub 仓库中的本文件内，并相应更新顶部的"最后更新"日期。

### 联系方式

源代码与问题跟踪：<https://github.com/N0I0C0K/PowerTranslator>

如有隐私相关问题，请在上述仓库中提交 Issue。
