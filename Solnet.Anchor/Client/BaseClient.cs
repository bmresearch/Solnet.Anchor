using Solnet.Rpc;
using Solnet.Rpc.Builders;
using Solnet.Rpc.Core.Http;
using Solnet.Rpc.Core.Sockets;
using Solnet.Rpc.Messages;
using Solnet.Rpc.Models;
using Solnet.Wallet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Solnet.Anchor.Client
{
    public abstract class BaseClient
    {
        public IRpcClient RpcClient { get; init; }

        public IStreamingRpcClient StreamingRpcClient { get; init; }

        public BaseClient(IRpcClient rpcClient, IStreamingRpcClient streamingRpcClient)
        {
            RpcClient = rpcClient;
            StreamingRpcClient = streamingRpcClient;
        }

        public static T DeserializeAccount<T>(byte[] data) where T : class
        {
            System.Reflection.MethodInfo m = typeof(T).GetMethod("Serialize",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static,
                null, new Type[] { typeof(byte) }, null);

            if (m == null)
                return null;
            return (T)m.Invoke(null, new object[] { data });
        }

        public async Task<ResultWrapper<T>> GetAccount<T>(string accountAddress) where T : class
        {
            var res = await RpcClient.GetAccountInfoAsync(accountAddress);

            if (res.WasSuccessful && res.Result?.Value.Data?.Count > 0)
            {
                return new ResultWrapper<T>(res, BaseClient.DeserializeAccount<T>(Convert.FromBase64String(res.Result.Value.Data[0])));
            }

            return new ResultWrapper<T>(res);
        }

        public async Task<SubscriptionState> SubscribeAccount<T>(string accountAddress, Action<SubscriptionState, ResponseValue<AccountInfo>, T> callback) where T : class
        {
            var res = await StreamingRpcClient.SubscribeAccountInfoAsync(accountAddress, new Action<SubscriptionState, ResponseValue<AccountInfo>>((s, e) =>
           {
               T parsingResult = null;

               if (e.Value?.Data?.Count > 0)
                   parsingResult = DeserializeAccount<T>(Convert.FromBase64String(e.Value.Data[0]));

               callback(s, e, parsingResult);
           }));

            return res;
        }

        protected async Task<RequestResult<string>> SignAndSendTransaction(TransactionInstruction instruction, PublicKey feePayer, Func<byte[], byte[], PublicKey> signingCallback)
        {
            TransactionBuilder tb = new TransactionBuilder();
            tb.AddInstruction(instruction);

            var recentHash = await RpcClient.GetRecentBlockHashAsync();

            tb.SetRecentBlockHash(recentHash.Result.Value.Blockhash);
            tb.SetFeePayer(feePayer);

            var payload = tb.CompileMessage();

            List<byte[]> signatures = new();
            var msg = Message.Deserialize(payload);

            for (int i = 0; i < msg.Header.RequiredSignatures; i++)
            {
                signatures.Add(signingCallback(payload, msg.AccountKeys[i]));
            }

            Transaction tx = Transaction.Populate(msg, signatures);

            var result = await RpcClient.SendTransactionAsync(tx.Serialize());

            return result;
        }
    }
}
