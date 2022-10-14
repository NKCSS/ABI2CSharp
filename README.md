
![Lumpi-Nick](https://rp.naw.io/img/lumpinick.png)
# Abi2CSharp
A utility to generate [C#](https://learn.microsoft.com/en-us/visualstudio/get-started/csharp/?view=vs-2022) code to help program against [Antelope](https://antelope.io/) chains based on [contract](https://github.com/blockmatic/antelope-contracts-list) [ABI](https://en.wikipedia.org/wiki/Application_binary_interface)'s

When you want to interact with the chain ([get_table_rows](https://developers.eos.io/manuals/eosjs/v21.0/how-to-guides/how-to-get-table-information)) or execute actions, you need to have structured models, contract names, action names, table names, etc. 

All of this data is available through a contract's ABI. What I propose is a utility that can 

- retrieve and read an ABI (specify contract name, code will interact with the chain to get the ABI) 
- generate C# code that gives constants with contract, table and action names 
- generates C# models so you can easily execute GetTableRows in [EosSharp](https://github.com/GetScatter/eos-sharp) for instance (you need to feed the type) and the data for Actions you want to execute. 

This will greatly help writing code to interact with the chain.

Funded by a [Wax Labs Grant](https://labs.wax.io/proposals/88)

## How to use

### Configuration of `App.Config` / `AutoMappedConfig`
Here's the example config the code ships with
```xml
<?xml version="1.0" encoding="utf-8" ?>
<configuration>
  <appSettings>
    <add key="chainId" value="1064487b3cd1a897ce03ae5b6a865651747e2e152090f99c1d19d44e01aea5a4"/>
    <add key="api" value="https://api.waxsweden.org"/>
    <add key="historyApi" value="https://api.waxeastern.cn"/>
    <add key="abiFolder" value="./abis/"/>
    <add key="abiFileSuffix" value="abis.json"/>
    <add key="abiFileSeparator" value="-"/>
    <add key="logFolder" value="./logs/"/>
    <add key="blockWarningLogFile" value="block-warning.txt"/>
    <add key="printAll" value="false"/>
    <add key="includeEosioModels" value="true"/>
    <add key="includeEosSharpTest" value="true"/>
    <add key="includeExtensions" value="true"/>
  </appSettings>
</configuration>
```
You should be fine using these values for [Wax](https://on.wax.io/wax-io/), but feel free to [pick](https://validate.eosnation.io/wax/reports/endpoints.html) [a different](https://wax.validationcore.io/reports/nodes/api) [History node](https://wax.eosio.online/endpoints).

The things you might want to change, are the `include*` settings. This inlines code into the generated file, but, depending on your preference, you might want to have some of those things as separate files in your solution. With all the `include*` settings set to `true`, it should generate a `.cs` file that you can include & compile without any errors 😅

You can use the code in various ways, but the easiest is probably to compile/execute the commandline application, and pass the `contract` you want to generate code for.

### Generate a file called `atomicmarket.cs` for the `atomicmarket` contract, and just use the latest available ABI
```
Abi2CSharp.exe atomicmarket
```
### Generate a file called `atomic.cs` for the `atomicmarket` contract the way it was a block `#12345`
```
Abi2CSharp.exe atomicmarket atomic 12345
```
The last two parameters can be swapped as long as one is a valid number and the other isn't 😊

**Note:** ABI's are cached; if you want to make sure they are re-downloaded from chain, delete the cache file inside the `./abis/` folder.


[![Twitter URL](https://img.shields.io/twitter/url/https/twitter.com/NKCSS.svg?style=social&label=Follow%20%40NKCSS)](https://twitter.com/NKCSS) 
[![Telegram](https://img.shields.io/badge/Telegram-2CA5E0?style=for-the-badge&logo=telegram&logoColor=white)
](https://t.me/NicksTechdom)[![YouTube](https://img.shields.io/badge/YouTube-%23FF0000.svg?style=for-the-badge&logo=YouTube&logoColor=white)
](https://nick.yt)