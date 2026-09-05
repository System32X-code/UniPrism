# UniPrism

[![Unity](https://img.shields.io/badge/Unity-2021.3+-brightgreen)](https://unity.com/releases/editor/qa/lts-releases?version=2021.3)

[English](README.md)

给 Unity 编辑器的每个窗口配上自己的背景图和配色。

## 安装

Package Manager → **+** → **Add package from git URL**：

```
https://github.com/System32X-code/UniPrism.git
```

## 使用

**窗口 → UniPrism**，选中一个窗口，然后：

- **图片** —— 该窗口的背景图。图片会编码进主题里，所以源资源移动了主题也不会失效。
- **底板染色** —— 乘算到窗口自己的面板上。调低 alpha，它那层不透明底就会变薄，背景图透出来。
- **文字与图标染色** —— 独立的一路，所以把底板调薄不会让文字发灰。
- **绘制在内容之上** —— 给那些底板怎么调都透不出来的窗口用。图片改画在内容上层，水印效果。

主题存在编辑器的 preferences 目录，跨工程、跨 Unity 版本跟着这台机器走。**导出**会写成一个
`.prism` 文件，方便迁移和分享。

**窗口 → UniPrism Diagnostics** 会打印绘制器实际看到的状态。这里所有的失败都是静默的——钩子没挂上、
标题对不上、图片没解码成功——所以怀疑哪里坏了之前先跑一下它。

## 原理，以及它做不到什么

**编辑器样式改不动。** 编辑器代码在静态构造里把样式解析一次就存成静态字段，其中约三分之二是
`new GUIStyle(...)` 深拷贝，跟皮肤彻底断开。就算直接去改那些真实实例——包括 Unity 自带的 IMGUI
调试器亲口指认"绘制时用的就是它"的那个对象——也没有任何变化：值写进去了、完整活过整个重绘、对象
也没找错，编辑器照样画出原样。在当前的 Unity 版本上，托管的 `GUIStyle` 背景与颜色字段已经不再
驱动渲染。

**有效的是染色。** IMGUI 在绘制时把 `GUI.backgroundColor` 乘到样式底板上、把 `GUI.contentColor`
乘到文字和图标上，而不是从样式里读回来。UniPrism 在窗口自身 OnGUI 的前后设置这两个值——那个位置同时
也是唯一可用的插入点：宿主已经画完不透明的边框底板、窗口还没开始画内容，并且在 `ResetGUIState`
之后（它在宿主 OnGUI 的第一句就把这些值全部重置，之前设的一切都白设）。

所以粒度是**整个窗口，不是逐个样式**。UniPrism 没法只把某个标签改成红色而不动旁边的按钮。这是
Unity 现在还认什么的边界，不是没做完的功能。

UniPrism 用到三样 Unity 没有公开的东西：枚举宿主、宿主当前显示的窗口、宿主用来绘制窗口的那个委托。
三样全部隔离在 [HostViewBridge](Editor/Painting/HostViewBridge.cs) 里，并且失败时静默降级——将来
Unity 改了名字，UniPrism 会停止工作并在诊断里说明原因，而不是每帧抛异常。

## 致谢

UniPrism 源于对 [piti6/UniSkin](https://github.com/piti6/UniSkin) 的排查，那个项目因为上面的原因在
Unity 2021.2+ 上已经不能用了。UniPrism 的机制与它不同，但整个调查是从那里开始的。

基于 MIT 协议发布，详见 [LICENSE](LICENSE)。
