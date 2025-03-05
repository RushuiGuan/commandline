# Albatross Framework
Albatross framework is an application framework built on top of Microsoft .Net.  It contains libraries and tools that solve common problems and patterns.

# Libraries
|Name|Description||
|-|-|-|
|[authentication](./authentication/Albatross.Authentication/) |Returns the identity of the current user|[![NuGet Version](https://img.shields.io/nuget/v/Albatross.Authentication)](https://www.nuget.org/packages/Albatross.Authentication)|
|[authentication.windows](./authentication/Albatross.Authentication.Windows/) |Returns the identity of the current windows user|[![NuGet Version](https://img.shields.io/nuget/v/Albatross.Authentication.Windows)](https://www.nuget.org/packages/Albatross.Authentication.Windows)|
|[authentication.aspnetcore](./authentication/Albatross.Authentication.AspNetCore/) |Returns the identity of the current user using httpcontext|[![NuGet Version](https://img.shields.io/nuget/v/Albatross.Authentication.AspNetCore)](https://www.nuget.org/packages/Albatross.Authentication.AspNetCore)|
|caching|||
|[codeanalysis](./codeanalysis/Albatross.CodeAnalysis/)|A code analysis library that provides syntax builders so that it is easier to create code generators using rosyln.||
|codegen|||
|collections|||
|[commandline](./commandline/Albatross.CommandLine)|An integration library that provdes depedency injection, configuration and logging support for [System.CommandLine](https://learn.microsoft.com/en-us/dotnet/standard/commandline/) library.|[![NuGet Version](https://img.shields.io/nuget/v/Albatross.CommandLine)](https://www.nuget.org/packages/Albatross.CommandLine)|
|[commandline codegen](./commandline/Albatross.CommandLine.CodeGen/)|An codegen library referenced by Albatross.CommandLine as a dev dependency.|[![NuGet Version](https://img.shields.io/nuget/v/Albatross.CommandLine.CodeGen)](https://www.nuget.org/packages/Albatross.CommandLine.CodeGen)|
|[config](./config/Albatross.Config)|Simplified configuration setup for your .Net applications.|[![NuGet Version](https://img.shields.io/nuget/v/Albatross.Config)](https://www.nuget.org/packages/Albatross.Config)|
|datelevel|||
|[dates](./dates/Albatross.Dates/)|Date and time utility library|[![NuGet Version](https://img.shields.io/nuget/v/Albatross.Dates)](https://www.nuget.org/packages/Albatross.Dates)|
|efcore|||
|[excel](./excel/Albatross.Hosting.Excel/)|ExcelDNA hosting library with logging, configuration and dependency injection.||
|[hosting](./hosting/Albatross.Hosting/)|Bootstrapping library that is used to create a web or service app|[![NuGet Version](https://img.shields.io/nuget/v/Albatross.Hosting)](https://www.nuget.org/packages/Albatross.Hosting)|
|io|||
|[logging](./logging/Albatross.Logging)|Quick logging setup for your .Net application using Serilog.|[![NuGet Version](https://img.shields.io/nuget/v/Albatross.Logging)](https://www.nuget.org/packages/Albatross.Logging)|
|math|||
|[messaging](./messaging/Albatross.Messaging)|Durable messaging library with ZeroMQ||
|[reflection](./reflection/Albatross.Reflection/)|A utility library that provides functionality related to .Net reflection.|[![NuGet Version](https://img.shields.io/nuget/v/Albatross.Reflection)](https://www.nuget.org/packages/Albatross.Reflection)|
|[serialization](./serialization/Albatross.Serialization/)|Provides additional functionalities for serialization using System.Text.Json|[![NuGet Version](https://img.shields.io/nuget/v/Albatross.Serialization)](https://www.nuget.org/packages/Albatross.Serialization)|
|[reqnroll plugin](./testing/Albatross.ReqnrollPlugin//)|A Reqnroll plugin library preconfigured with configuration, logging and dependency injection.|[![NuGet Version](https://img.shields.io/nuget/v/Albatross.ReqnrollPlugin)](https://www.nuget.org/packages/Albatross.ReqnrollPlugin)|
|[text](./text/Albatross.Text/)|A string manipulation library|[![NuGet Version](https://img.shields.io/nuget/v/Albatross.Text)](https://www.nuget.org/packages/Albatross.Text)|
|threading|||
|[webclient](./webclient/Albatross.WebClient/)|Extends the functionality of .net HttpClient class||