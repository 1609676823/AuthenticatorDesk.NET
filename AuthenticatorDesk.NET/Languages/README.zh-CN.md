# AuthenticatorDesk 语言包

[English](README.md)

语言包是可直接编辑和分发的 UTF-8 JSON 文件，不需要编译 DLL。程序启动时会依次从可执行
文件旁的 `Languages` 目录和 `%LocalAppData%\AuthenticatorDesk\Languages` 加载语言包。
用户目录中的同语言文件优先级更高，适合在没有程序目录写权限时安装或更新翻译。

每一个有效的语言包都会自动出现在**设置 → 语言**中。更改所选语言会重启应用。

## 内置语言

仓库当前包含：

| 区域代码 | 语言 |
| --- | --- |
| `en` | 英语 |
| `zh-Hans` | 简体中文 |
| `zh-Hant` | 繁体中文 |
| `ja` | 日语 |
| `ko` | 韩语 |
| `de` | 德语 |
| `fr` | 法语 |
| `es` | 西班牙语 |
| `pt-BR` | 巴西葡萄牙语 |
| `ru` | 俄语 |

英语是源语言基线。所有内置语言包必须包含与 `en.json` 相同的完整键集合。

## 新增语言

1. 复制 `en.json`，并将文件重命名为标准 BCP-47 区域代码，例如 `it.json`、
   `nl.json` 或 `pt-PT.json`。
2. 修改元数据：
   - `schemaVersion` 保持为 `1`；
   - `locale` 必须与不含 `.json` 的文件名完全一致；
   - `name` 使用英文语言名；
   - `nativeName` 使用该语言自己的名称；
   - 可选的 `authors` 数组可以记录译者署名。
3. 只翻译 `strings` 中的值，切勿修改键名。
4. 完整保留 `{0}`、`{1}` 等格式项及其出现次数。译文可以调整顺序，但不得新增或修改
   对齐宽度及格式说明符。
5. 适当保留产品名、服务商名、协议名和算法名，包括 AuthenticatorDesk、WinAuth、TOTP、
   HOTP、Base32、AES-GCM 与 SHA-256。
6. 启动程序并在设置中选择新语言。

社区语言包可以只包含部分键，缺少或无效的值会回退到内嵌英语。提交为内置语言的语言包
必须包含完整的英文键集合，并通过测试套件。

## 匹配与回退

默认选项跟随 Windows 当前显示语言。程序按完整区域、脚本或父语言，最后到英语的顺序
匹配。例如：

- `zh-CN`、`zh-SG` → `zh-Hans`
- `zh-TW`、`zh-HK`、`zh-MO` → `zh-Hant`
- `fr-CA` → `fr`
- 未提供或损坏的语言包 → `en`

语言偏好保存在 `%LocalAppData%\AuthenticatorDesk\ui-preferences.json`，与保险库和验证器
数据分离。因此，启动和保险库解锁窗口可以在保险库打开前使用所选语言。

## 格式与安全限制

JSON Schema 请参阅 [language-pack.schema.json](language-pack.schema.json)。加载器只读取
JSON 文本，不加载脚本或程序集，并执行以下限制：

- 单个文件不超过 512 KiB；
- 最多 5,000 个字符串；
- 单个键最长 256 个字符；
- 单个译文最长 4,096 个字符；
- 文件名与 `locale` 必须一致；
- 完整格式项（参数、对齐和格式说明符）必须与内嵌英文基线一致。

解析失败、超过限制或占位符不兼容的语言包不会阻止应用启动；受影响内容会安全地回退到
英语。

## 测试贡献

请从仓库根目录在 Windows 和 .NET 10 SDK 环境中运行完整测试套件：

```powershell
dotnet test .\AuthenticatorDesk.NET.slnx -c Release
```

还应在应用中选择该语言，检查主页、条目编辑器、设置、启动和解锁窗口、导入导出菜单及
窄窗口布局。确认快捷键、格式占位符、标点和自动换行保持可用。

当前界面已针对从左到右书写的语言完成适配和视觉检查。我们欢迎阿拉伯语、希伯来语等
从右到左语言的翻译，但要成为完整内置语言，还需要同步完成 WinForms/AntdUI 的 RTL 布局
工作和视觉测试。

一般贡献要求请参阅仓库的[中文贡献指南](../../CONTRIBUTING.zh-CN.md)。
