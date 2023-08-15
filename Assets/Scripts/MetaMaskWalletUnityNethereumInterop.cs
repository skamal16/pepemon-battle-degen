//PoC Interop to integrate Nethereum and MM Sdk in Unity.
using System;
using System.Numerics;
using System.Threading.Tasks;
using MetaMask.Models;
using MetaMask.Unity;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;
using Nethereum.JsonRpc.Client;
using Nethereum.JsonRpc.Client.RpcMessages;
using Nethereum.RPC.AccountSigning;
using Nethereum.RPC.HostWallet;
using Nethereum.Unity.Rpc;
using Nethereum.Web3;
using Newtonsoft.Json;
using UnityEngine;
using MetaMask;
using Nethereum.Metamask;

/*
        public async Task QueryAndSendErc20Async()
        {
            var web3 = MetamaskUnityWalletWeb3Factory.CreateWeb3("https://goerli.infura.io/v3/-");
          

            var contractAddress = "0x95dde605a7f09f7595651482cee9dded92d3cd84";

            var balanceOfFunctionMessage = new BalanceOfFunction()
            {
                Owner = MetaMaskUnity.Instance.Wallet.SelectedAddress,
            };

            var balanceHandler = web3.Eth.GetContractQueryHandler<BalanceOfFunction>();

            var balance = await balanceHandler.QueryAsync<BigInteger>(contractAddress, balanceOfFunctionMessage);

            Debug.LogWarning("Balance of owner address: " + balance);


            var receiverAddress = "0xde0B295669a9FD93d5F28D9Ec85E40f4cb697BAe";
            var transferHandler = web3.Eth.GetContractTransactionHandler<TransferFunction>();

            var transfer = new TransferFunction()
            {
                To = receiverAddress,
                TokenAmount = 100
            };

            var transactionTransferTxn =
                await transferHandler.SendRequestAsync(contractAddress, transfer);
            Debug.LogWarning("Transaction hash transfer is: " + transactionTransferTxn);

    
        }

        /// <summary>Signs a message with the user's private key.</summary>
        /// <param name="msgParams">The message to sign.</param>
        /// <exception cref="InvalidOperationException">Thrown when the application isn't in foreground.</exception>
        public async void Sign()
        {
            string msgParams = "{\"domain\":{\"chainId\":5,\"name\":\"Ether Mail\",\"verifyingContract\":\"0xCcCCccccCCCCcCCCCCCcCcCccCcCCCcCcccccccC\",\"version\":\"1\"},\"message\":{\"contents\":\"Hello, Bob!\",\"from\":{\"name\":\"Cow\",\"wallets\":[\"0xCD2a3d9F938E13CD947Ec05AbC7FE734Df8DD826\",\"0xDeaDbeefdEAdbeefdEadbEEFdeadbeEFdEaDbeeF\"]},\"to\":[{\"name\":\"Bob\",\"wallets\":[\"0xbBbBBBBbbBBBbbbBbbBbbbbBBbBbbbbBbBbbBBbB\",\"0xB0BdaBea57B0BDABeA57b0bdABEA57b0BDabEa57\",\"0xB0B0b0b0b0b0B000000000000000000000000000\"]}]},\"primaryType\":\"Mail\",\"types\":{\"EIP712Domain\":[{\"name\":\"name\",\"type\":\"string\"},{\"name\":\"version\",\"type\":\"string\"},{\"name\":\"chainId\",\"type\":\"uint256\"},{\"name\":\"verifyingContract\",\"type\":\"address\"}],\"Group\":[{\"name\":\"name\",\"type\":\"string\"},{\"name\":\"members\",\"type\":\"Person[]\"}],\"Mail\":[{\"name\":\"from\",\"type\":\"Person\"},{\"name\":\"to\",\"type\":\"Person[]\"},{\"name\":\"contents\",\"type\":\"string\"}],\"Person\":[{\"name\":\"name\",\"type\":\"string\"},{\"name\":\"wallets\",\"type\":\"address[]\"}]}}";

            var web3 = MetamaskUnityWalletWeb3Factory.CreateWeb3("https://goerli.infura.io/v3/-");
            var response = await web3.Eth.AccountSigning.SignTypedDataV4.SendRequestAsync(msgParams);
            Debug.Log(response);

        }

*/

public class MetamaskUnityWalletWeb3Factory
    {
        public static IWeb3 CreateWeb3(string url)
        {
            var web3 = new Web3(new UnityWebRequestRpcTaskClient(new Uri(url)));
            var interceptor = new MetamaskInterceptor(new MetaMaskWalletNethereumInterop(MetaMaskUnity.Instance.Wallet), true);
            interceptor.SelectedAccount = MetaMaskUnity.Instance.Wallet.SelectedAddress;
            web3.Client.OverridingRequestInterceptor = interceptor;
            return web3;
        }

    }

    public class MetaMaskWalletNethereumInterop : IMetamaskInterop
    {
        public MetaMaskWalletNethereumInterop(MetaMaskWallet metaMaskWallet)
        {
            MetaMaskWallet = metaMaskWallet;
        }

        public MetaMaskWallet MetaMaskWallet { get; }

        public Task<bool> CheckMetamaskAvailability()
        {
            return Task.FromResult<bool>(MetaMaskWallet.IsConnected);
        }

        public async Task<string> EnableEthereumAsync()
        {
            if (MetaMaskWallet.IsConnected)
            {
                Debug.Log("EnableEthereumAsync: Wallet Connected.");
                return MetaMaskWallet.SelectedAddress;
            }
            else
            {
                Debug.Log("EnableEthereumAsync: Wallet Not Connected.");
                var requestAccounts = new EthRequestAccounts();
                var request = requestAccounts.BuildRequest();
                var metamaskRequest = new RpcRequestMessage(request.Id, request.Method, request.RawParameters);
                var response = await SendAsync(metamaskRequest);
                return MetaMaskWallet.SelectedAddress;
            }
        }

        public Task<string> GetSelectedAddress()
        {
            return Task.FromResult(MetaMaskWallet.SelectedAddress);
        }

        public async Task<RpcResponseMessage> SendAsync(RpcRequestMessage rpcRequestMessage)
        {
            try
            {
                var metamaskRequest = new MetaMaskEthereumRequest();
                metamaskRequest.Method = rpcRequestMessage.Method;
                metamaskRequest.Id = rpcRequestMessage.Id.ToString();
                metamaskRequest.Parameters = rpcRequestMessage.RawParameters;
                var jsonResponse = await MetaMaskWallet.Request(metamaskRequest);
                var response =
                    @$"{{""jsonrpc"":""2.0"",""id"":""{rpcRequestMessage.Id.ToString()}"",""result"":""{jsonResponse.ToString()}""}}";
                return JsonConvert.DeserializeObject<RpcResponseMessage>(response);
            }
            catch(Exception ex)
            {
                try
                {
                    var rpcError = JsonConvert.DeserializeObject<Nethereum.JsonRpc.Client.RpcMessages.RpcError>(ex.Message);
                    return new RpcResponseMessage(rpcRequestMessage.Id, rpcError);
                    
                }
                catch
                {
                    try
                    {
                        var rpcErrorCpde = JsonConvert.DeserializeObject<OnlyCodeRpcError>(ex.Message);
                        var error = @$"{{""code"":""{rpcErrorCpde.Code}"",""message"":""""}}";
                        var rpcError = JsonConvert.DeserializeObject<Nethereum.JsonRpc.Client.RpcMessages.RpcError>(error);
                        if (rpcError.Code == 0)
                        {
                            throw new RpcClientUnknownException("Error occurred when trying to send rpc requests(s): " + rpcRequestMessage.Method, ex);
                        }
                        else
                        {
                            return new RpcResponseMessage(rpcRequestMessage.Id, rpcError);
                        }
                    }
                    catch
                    {
                        throw new RpcClientUnknownException("Error occurred when trying to send rpc requests(s): " + rpcRequestMessage.Method, ex);
                    }
                }
            }
        }

        private class OnlyCodeRpcError
        {
            [JsonProperty("code")]
            public int Code { get; private set; }
        }

        public Task<RpcResponseMessage> SendTransactionAsync(MetamaskRpcRequestMessage request)
        {
            var metamaskRequest = new MetamaskRpcRequestMessage(request.Id, request.Method, MetaMaskWallet.SelectedAddress, request.RawParameters);
            return SendAsync(metamaskRequest);
           
        }

        public async Task<string> SignAsync(string utf8Hex)
        {
            var personalSign = new EthPersonalSign();
            var request = personalSign.BuildRequest(new Nethereum.Hex.HexTypes.HexUTF8String(utf8Hex));
            var metamaskRequest = new MetamaskRpcRequestMessage(request.Id, request.Method, MetaMaskWallet.SelectedAddress, request.RawParameters);
            var response = await SendAsync(metamaskRequest);
            return ConvertResponse<string>(response);
        }

        private void HandleRpcError(RpcResponseMessage response)
        {
            if (response.HasError)
                throw new RpcResponseException(new Nethereum.JsonRpc.Client.RpcError(response.Error.Code, response.Error.Message,
                    response.Error.Data));
        }

        private T ConvertResponse<T>(RpcResponseMessage response,
           string route = null)
        {
            HandleRpcError(response);
            try
            {
                return response.GetResult<T>();
            }
            catch (FormatException formatException)
            {
                throw new RpcResponseFormatException("Invalid format found in RPC response", formatException);
            }
        }
    }