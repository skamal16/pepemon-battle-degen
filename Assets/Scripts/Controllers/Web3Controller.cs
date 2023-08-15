﻿using Nethereum.Hex.HexTypes;
using Nethereum.Unity.Contracts;
using Nethereum.Unity.Rpc;
using Nethereum.Web3.Accounts;
using UnityEngine;
using UnityEngine.Events;
//#if !UNITY_EDITOR
//using Nethereum.Unity.Metamask;
using Nethereum.RPC.HostWallet;
using System.Collections.Generic;
//#endif
using Nethereum.RPC.Eth.DTOs;
using Cysharp.Threading.Tasks;
using System.Threading.Tasks;
using UnityEngine.UI;
using Sirenix.OdinInspector;
using System.Linq;
using System;
using MetaMask.Unity;
using Nethereum.Signer;
using Nethereum.Web3;
using UnityEngine.Assertions;

public class Web3Controller : MonoBehaviour
{
    public static Web3Controller instance;

    public Web3Settings settings;
    public UnityEvent onWalletConnected;
    public int CurrentChainId { get; private set; } = 0;
    public string SelectedAccountAddress { get; private set; }

#if UNITY_EDITOR
    #region Unity editor button for updating hardhat contracts addresses
    [Serializable]
    struct DeploymentJson
    {
        public string address;
    }

    [Button(), PropertyTooltip("Update hardhat's addresses (chainId 31337) from localhost deployment," +
        "this way there is no need to copy the contracts' addresses from the terminal")]
    private void UpdateHardhatAddresses()
    {
        // use hardhat local deployment assuming that the battle-contracts project is on ../
        var deployments = @"..\battle-contracts\deployments\localhost";
        
        // get hardhat config
        var chain = settings.chains.FirstOrDefault(x => x.chainId == 31337);
        var index = Array.IndexOf(settings.chains, chain);

        chain.pepemonBattleAddress = JsonUtility.FromJson<DeploymentJson>(
            System.IO.File.ReadAllText($@"{deployments}\PepemonBattle.json")).address;
        Debug.Log("Set pepemonBattleAddress = " + chain.pepemonBattleAddress);

        chain.pepemonCardDeckAddress = JsonUtility.FromJson<DeploymentJson>(
            System.IO.File.ReadAllText($@"{deployments}\PepemonCardDeck.json")).address;
        Debug.Log("Set pepemonCardDeckAddress = " + chain.pepemonCardDeckAddress);

        chain.pepemonMatchmakerAddresses[0] = JsonUtility.FromJson<DeploymentJson>(
            System.IO.File.ReadAllText($@"{deployments}\PepemonMatchmaker.json")).address;
        Debug.Log("Set pepemonMatchmakerAddresses[0] = " + chain.pepemonMatchmakerAddresses[0]);

        settings.chains[index] = chain;
    }
    #endregion
#endif

//#if !UNITY_EDITOR
    private MetaMaskWalletNethereumInterop interop;
    //private bool _isMetamaskInitialised = false;
//#endif

    private void Awake()
    {
        if (instance == null)
        {
            DontDestroyOnLoad(this);
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }
        CurrentChainId = settings.defaultChainId;
    }

    public void ConnectWallet()
    {
        Debug.Log("Trying to connect");
//#if !UNITY_EDITOR
        OpenMetamaskConnectDialog();
//#else
        //Account debugAccount = new Account(settings.debugPrivateKey);
        //NewAccountSelected(debugAccount.Address);
        //ChainChanged(string.Format("0x{0:X}", settings.defaultChainId));
        //onWalletConnected?.Invoke();
//#endif
    }

    public Web3Settings.Web3ChainConfig GetChainConfig()
    {
        return settings.GetChainConfig(CurrentChainId);
    }

    /// <summary>
    /// Used with QueryUnityRequest to query contract functions (READ operations)
    /// </summary>
    public IUnityRpcRequestClientFactory GetUnityRpcRequestClientFactory()
    {
#if UNITY_WEBGL
        if (IsWebGL())
        {
            if (MetamaskWebglInterop.IsMetamaskAvailable())
            {
                return new MetamaskWebglCoroutineRequestRpcClientFactory(SelectedAccountAddress, null, 60000);
            }
            else
            {
                // DisplayError("Metamask is not available, please install it");
                return null;
            }
        }
        else
        {
#endif
            return new UnityWebRequestRpcClientFactory(settings.debugRpcUrl);
#if UNITY_WEBGL
        }
#endif
    }

    public bool IsWebGL()
    {
#if UNITY_WEBGL
        return true;
#else
      return false;
#endif
    }

    /// <summary>
    /// Used with QueryUnityRequest to query contract functions (READ operations)
    /// </summary>
    public IUnityRpcRequestClientFactory GetReadOnlyRpcRequestClientFactory()
    {
        return new UnityWebRequestRpcClientFactory(settings.readOnlyRpcUrl);
    }

    /// <summary>
    /// Used to invoke contract functions (WRITE operations)
    /// </summary>
    public IContractTransactionUnityRequest GetContractTransactionUnityRequest()
    {
#if UNITY_WEBGL

        if (IsWebGL())
        {
            if (MetamaskWebglInterop.IsMetamaskAvailable())
            {
                return new MetamaskTransactionCoroutineUnityRequest(_selectedAccountAddress, GetUnityRpcRequestClientFactory());
            }
            else
            {
                DisplayError("Metamask is not available, please install it");
                return null;
            }
        }
        else
        {
#endif
        var account = new Account(settings.debugPrivateKey);
        NewAccountSelected(account.Address);
        return new TransactionSignedUnityRequest(settings.debugRpcUrl, settings.debugPrivateKey, settings.defaultChainId);
#if UNITY_WEBGL
        }
#endif
    }

    // connect wallet in WebGL
    private void OpenMetamaskConnectDialog()
    {
//#if !UNITY_EDITOR

        MetaMaskUnity.Instance.Initialize();

        //IWeb3 web3 = MetamaskUnityWalletWeb3Factory.CreateWeb3(settings.readOnlyRpcUrl);

        if (!MetaMaskUnity.Instance.Wallet.IsConnected) MetaMaskUnity.Instance.Wallet.Connect();
        else
        {
            Debug.Log("WALLET IS ALREADY CONNECTED");
            OnWalletConnected();
        }

        void Connected(object sender, EventArgs e) {
            Debug.Log("WALLET CONNECTED");
            OnWalletConnected();
        }

        async void OnWalletConnected()
        {
            interop = new MetaMaskWalletNethereumInterop(MetaMaskUnity.Instance.Wallet);

            Debug.Log("Connected Successfully");
            Debug.Log("Attempting to enable ethereum");
            string addressSelected = await interop.EnableEthereumAsync();

            onWalletConnected?.Invoke();
            Assert.IsNotNull(onWalletConnected);
            Debug.Log("onWalletConnected Called");
            NewAccountSelected(addressSelected);
        }

        MetaMaskUnity.Instance.Wallet.WalletConnected+= Connected;

        //for (int i = 0; i < 10; i++)
        //{
        //    if (connected) break;
        //    await wait;
        //    if (connected) break;
        //    wait.Reset();
        //    Debug.Log("Waiting to Connect");
        //}

        //#endif
        //DisplayError("Metamask Connect Dialog failed to open");
    }

    //callback from js
    //public async void EthereumEnabled(string addressSelected)
    //{
    //    //#if !UNITY_EDITOR
    //    if (!_isMetamaskInitialised)
    //    {
    //        interop.EthereumInit(gameObject.name, nameof(NewAccountSelected), nameof(ChainChanged));
    //        interop.GetChainId(gameObject.name, nameof(ChainChanged), nameof(DisplayError));
    //        _isMetamaskInitialised = true;
    //    }
    //    onWalletConnected?.Invoke();
    //    NewAccountSelected(addressSelected);
    //    //#else
    //    await Task.CompletedTask;
    //    //#endif
    //}

    // callback from js
    public void NewAccountSelected(string accountAddress)
    {
        SelectedAccountAddress = accountAddress;
        Debug.Log($"Account changed to {SelectedAccountAddress}");
    }

    /// <summary>
    /// Displays a popup in metamask asking the user to confirm changing the current network
    /// </summary>
    /// <param name="chainId">Network's chain id to switch to</param>
    /// <returns>Task that must be awaited for the popup to appear</returns>
    private async Task SwitchChain(int chainId)
    {
//#if !UNITY_EDITOR
        var addRequest = new WalletAddEthereumChainUnityRequest(GetUnityRpcRequestClientFactory());

        var config = settings.GetChainConfig(chainId);
        var chainParams = new AddEthereumChainParameter
        {
            ChainId = new HexBigInteger(chainId),
            ChainName = config.chainName,
            RpcUrls = new List<string> { config.rpcUrl },
            NativeCurrency = new NativeCurrency
            {
                Name = config.chainCurrencyName,
                Symbol = config.chainCurrencySymbol,
                Decimals = config.chainCurrencyDecimals
            }
        };
        await addRequest.SendRequest(chainParams);
//#else
        //await Task.CompletedTask;
//#endif
    }

    // callback from js
    public async void ChainChanged(string chainId)
    {
        CurrentChainId = (int)new HexBigInteger(chainId).Value;
        Debug.Log($"Changed chain to {CurrentChainId} (hex: {chainId})");

        if (CurrentChainId != settings.defaultChainId)
        {
            Debug.Log($"Attempting to switch chain to {settings.defaultChainId}");
            await SwitchChain(settings.defaultChainId);
        }
    }

    public void DisplayError(string errorMessage)
    {
        Debug.LogError(errorMessage);
    }

    public static async Task<TransactionReceipt> GetTransactionReceipt(string transactionHash)
    {
        var request = new TransactionReceiptPollingRequest(instance.GetUnityRpcRequestClientFactory());
        await request.PollForReceipt(transactionHash, 0.25f);
        if (request.Result != null)
        {
            return request.Result;
        }
        throw request.Exception;
    }
}
