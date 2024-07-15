# C#转换Lua文档生成器

## 介绍

该库的作用是为使用xlua或其他类似C#库生成一份符合 [LuaCATS](https://luals.github.io/wiki/annotations/) 注释标准（兼容 `emmylua`以及 `sumneko.lua`的Lua类型注释）的文档。

注：该库仅生成注释，调用与交互请参考lua库内的说明。

## 当前实现功能

* [X] 为类型生成类型文档
* [X] 为字段与属性生成字段标注
* [X] 为成员方法与静态方法生成函数标注
* [X] 支持生成泛型类标注，并生成多个已有泛型类的文档（为单例类型提供支持）
* [X] 支持生成扩展方法标注，会生成静态方法以及对应类型上的实例方法（当前对接口支持不佳）
* [X] 支持为类型可选参数添加对应标注
* [X] 支持过滤不生成的类型或程序集

## 使用方法

构建或从Release下载程序。

在控制台通过 ` .\CS2LuaDoc.Cli.exe [SolutionPath] --args`传入参数。

当前可传入的参数有：

| 参数                                    | 必须项 | 说明                                 |
| --------------------------------------- | :----: | ------------------------------------ |
| `[SolutionPath]`                        |   √    |                                      |
| -o\|--output `<OutputPath>`             |   √    | 程序输出目录                         |
| --exclude-namespace`<ExcludeNamespace>` |        | 排除的命名空间                       |
| --exclude-assembly `<ExcludeAssembly>`  |        | 排除的程序集                         |
| --exclude-assembly <ExcludeAssembly>    |        | 排除的程序集                         |
| --include-assembly <IncludeAssembly>    |        | 包含的程序集                         |
| -p\|--public-only                       |        | 是否仅导出public类、方法、字段和属性 |