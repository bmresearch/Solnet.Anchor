using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jet;
using Solnet.Rpc;
using Solnet.Wallet;
using SequenceEnforcer;

namespace Solnet.Anchor.Examples
{
    public class Program
    {
        public static void Main()
        {
            var program = new PublicKey("GDDMwNyyx8uB6zrqwBFHjLLG3TBYk2F8Az4yrQC5RzMp");
            var mn = "redacted";
            System.Diagnostics.Debugger.Launch();
            var wallet = new Wallet.Wallet(mn);

            var rpc = ClientFactory.GetClient("https://ssc-dao.genesysgo.net");
            var rpcStreaming = ClientFactory.GetStreamingClient(Cluster.MainNet);
            /*
            JetClient c = new JetClient(rpc, rpcStreaming);

            var markets = c.GetMarketsAsync();
            var obligations = c.GetObligationsAsync();
            var reserves = c.GetReservesAsync();
            
            Task.WaitAll(markets, obligations, reserves);

            var bal = rpc.GetBalance(wallet.Account.PublicKey);



            SequenceEnforcerClient seq = new SequenceEnforcerClient(rpc, rpcStreaming);

            var sym = "test";
            PublicKey.TryFindProgramAddress(new[] { Encoding.UTF8.GetBytes(sym), wallet.Account.PublicKey.KeyBytes }, program, out var newAcc, out var bump);

            var initAccounts = new SequenceEnforcer.Program.InitializeAccounts
            {
                Authority = wallet.Account.PublicKey,
                SequenceAccount = newAcc,
                SystemProgram = Solnet.Programs.SystemProgram.ProgramIdKey
            };

            var res = seq.SendInitializeAsync(initAccounts, bump, sym, wallet.Account,
                (payload, pk) => wallet.Sign(payload)).Result;

            //can also get instruction for compossability
            var ix = SequenceEnforcer.Program.SequenceEnforcerProgram.Initialize( initAccounts, bump, sym);

            ulong sequence = 0ul;

            var ix2 = SequenceEnforcer.Program.SequenceEnforcerProgram.CheckAndSetSequenceNumber( new SequenceEnforcer.Program.CheckAndSetSequenceNumberAccounts() { Authority = wallet.Account.PublicKey, SequenceAccount = newAcc }, sequence);

            var ix3 = SequenceEnforcer.Program.SequenceEnforcerProgram.ResetSequenceNumber( new SequenceEnforcer.Program.ResetSequenceNumberAccounts() { Authority = wallet.Account.PublicKey, SequenceAccount = newAcc }, sequence);

            var tx = rpc.SendTransaction("");
            */

            Console.ReadLine();
        }
    }
}
