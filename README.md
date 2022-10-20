
![Lumpi-Nick](https://rp.naw.io/img/lumpinick.png)
# Abi2CSharp
A utility to generate [C#](https://learn.microsoft.com/en-us/visualstudio/get-started/csharp/?view=vs-2022) code to help program against [Antelope](https://antelope.io/) chains based on [contract](https://github.com/blockmatic/antelope-contracts-list) [ABI](https://en.wikipedia.org/wiki/Application_binary_interface)'s

When you want to interact with the chain ([get_table_rows](https://developers.eos.io/manuals/eosjs/v21.0/how-to-guides/how-to-get-table-information)) or execute actions, you need to have structured models, contract names, action names, table names, etc. 

All of this data is available through a contract's ABI. What I propose is a utility that can 

- retrieve and read an ABI (specify contract name, code will interact with the chain to get the ABI) 
- generate C# code that gives constants with contract, table and action names 
- generates C# models so you can easily execute GetTableRows in [EosSharp](https://github.com/NKCSS/eos-sharp) for instance (you need to feed the type) and the data for Actions you want to execute. 

NOTE: I forked EosSharp to fix some small things; using the default package might not work for you, so be ware/apply patches where needed.

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


### Example program using generated `eosio` code

```csharp
using EosSharp;
using EosSharp.Core;
using EosSharp.Core.Api.v1;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Abi2CSharp
{
    public static class VoterClaim
    {
        const int secondsPerDay = 60 * 60 * 24;
        // Feel free to just hard-code those values here/use your own way to load your wallet data.
        //NOTE: Make sure to never commit sensitive data to public repositories😅
        static (string address, string pub, string priv) accountInfo = (AutoMappedConfig.account, AutoMappedConfig.publicKey, AutoMappedConfig.privateKey);
        const string transactionMarker = "\"transaction_id\":";
        static int start;
        static string GetTransactionIdFromJSON(string json) => (start = json.IndexOf(transactionMarker) + transactionMarker.Length + 1) > transactionMarker.Length ? json.Substring(start, json.IndexOf(',', start) - (start + 1)) : null;
        public static async Task CheckAndClaimVote()
        {
            Model.eosio.Name wallet = accountInfo.address;
            var api = new EosApi(new EosConfigurator()
            {
                SignProvider = new EosSharp.Core.Providers.DefaultSignProvider(accountInfo.priv),
                HttpEndpoint = AutoMappedConfig.api,
                ChainId = AutoMappedConfig.chainId
            }, new HttpHandler());
            var wax = new Eos(api.Config);
            var abiSerializer = new EosSharp.Core.Providers.AbiSerializationProvider(api);

            var voteInfo = (await Contracts.eosio.Tables.voters.Query(api, lowerBound: wallet, upperBound: wallet)).rows.FirstOrDefault(); // There might not be vote info
            Console.WriteLine($"Last claim: {voteInfo?.last_claim_time.Moment:yyyy-MM-dd HH:mm:ss}");
            if (voteInfo == null) 
            {
                Console.WriteLine("No vote info found; make sure to vote first!");
            }
            else
            {
                var waxGlobal = (await Contracts.eosio.Tables.global.Query(api)).rows[0];
                Console.WriteLine(JsonConvert.SerializeObject(waxGlobal));
                var secondsElapsed = DateTime.UtcNow.Subtract(voteInfo.unpaid_voteshare_last_updated.Moment).TotalSeconds;
                if (secondsElapsed > secondsPerDay)
                {
                    var unpaid_voteshare = voteInfo.unpaid_voteshare + (voteInfo.unpaid_voteshare_change_rate * secondsElapsed);
                    var reward = waxGlobal.voters_bucket * (unpaid_voteshare / waxGlobal.total_unpaid_voteshare);
                    Console.WriteLine($"Voter Claimable: {(reward / 1e8):F8} WAX");
                    var action = Contracts.eosio.Requests.voterclaim.CreateAction(wallet, new Contracts.eosio.Types.voterclaim { owner = wallet });
                    var trx = new Transaction()
                    {
                        max_net_usage_words = 0,
                        max_cpu_usage_ms = 0,
                        delay_sec = 0,
                        context_free_actions = new List<EosSharp.Core.Api.v1.Action>(),
                        transaction_extensions = new List<Extension>(),
                        actions = new List<EosSharp.Core.Api.v1.Action> { action },
                    };
                    var packedTrx = await abiSerializer.SerializePackedTransaction(trx);
                    var requiredKeys = new List<string>() { accountInfo.pub };
                    var signatures = await api.Config.SignProvider.Sign(api.Config.ChainId, requiredKeys, packedTrx);
                    try
                    {
                        string result = await wax.CreateTransaction(trx);
                        string tx = GetTransactionIdFromJSON(result);
                        if (!string.IsNullOrWhiteSpace(tx)) Console.WriteLine(tx);
                        else Console.WriteLine(result);
                    }
                    catch (EosSharp.Core.Exceptions.ApiErrorException aeex)
                    {
                        Console.WriteLine("An error occured:");
                        var parts = aeex.error.details.FirstOrDefault()?.message?.Split(':');
                        if ((parts?.Length ?? 0) > 1)
                        {
                            Console.WriteLine(parts[1].Trim());
                        }
                        else
                        {
                            Console.WriteLine(string.Join(Environment.NewLine, aeex.error.details.Select(x => x.message)));
                        }
                    }
                }
                else
                {
                    Console.WriteLine($"Too early to claim voter pay (wait {voteInfo.unpaid_voteshare_last_updated.Moment.AddDays(1).Subtract(DateTime.UtcNow)})");
                }
            }
        }
    }
}
```