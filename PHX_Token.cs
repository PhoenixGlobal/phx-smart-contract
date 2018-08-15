using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using Neo.SmartContract.Framework.Services.System;
using System;
using System.ComponentModel;
using System.Numerics;

namespace PHX_Token
{
    public class PHX_Token : SmartContract
    {
        //Token Settings
        [DisplayName("name")]
        public static string Name() => "Red Pulse Phoenix Token";

        [DisplayName("symbol")]
        public static string Symbol() => "PHX";
        
        [DisplayName("decimals")]
        public static byte Decimals() => 8;
        private const ulong factor = 100000000; //decided by Decimals()
        
        [DisplayName("supportedStandards")]
        public static string SupportedStandards() => "{\"NEP-5\", \"NEP-10\"}";

        private const ulong totalSupply_initial = 1358371250 * factor;

        public static readonly byte[] wallet_red_pulse_platform = "thisisaplaceholderwalletaddress123".ToScriptHash();
        public static readonly byte[] wallet_red_pulse_platform_airdrop = "thisisaplaceholderwalletaddress234".ToScriptHash();
        public static readonly byte[] wallet_red_pulse_platform_inflation = "thisisaplaceholderwalletaddress345".ToScriptHash();

        // Storage key prefix for nep-5
        private static readonly byte[] prefix_balance = { 0x11 };
        private static readonly byte[] prefix_allowance = { 0x12 };

        // Storage key prefixes for storing proof values
        private static readonly byte[] proof_owner_prefix = { 0x21 };
        private static readonly byte[] proof_creator_prefix = { 0x22 };
        private static readonly byte[] proof_signer_prefix = { 0x23 };
        private static readonly byte[] proof_timestampCreated_prefix = { 0x24 };
        private static readonly byte[] proof_timestampTransferred_prefix = { 0x25 };

        // Events for NEP-5
        [DisplayName("transfer")]
        public static event Action<byte[], byte[], BigInteger> Transferred;

        [DisplayName("approve")]
        public static event Action<byte[], byte[], BigInteger> OnApproved;

        // Events for proof of ownership
        [DisplayName("proofCreated")]
        public static event Action<byte[], byte[], byte[]> ProofCreated;  // hash, owner, signer

        [DisplayName("proofTransferred")]
        public static event Action<byte[], byte[], byte[]> ProofTransferred;  // hash, owner_old, owner_new

        [DisplayName("proofModified")]
        public static event Action<byte[], byte[]> ProofModified;  // hash, owner_old

        public static Object Main(string operation, params object[] args)
        {
            if (Runtime.Trigger == TriggerType.Verification)
            {
                return Runtime.CheckWitness(wallet_red_pulse_platform);
            }
            else if (Runtime.Trigger == TriggerType.Application)
            {
                if (operation == "deploy") return Deploy();
                if (operation == "supportedStandards") return SupportedStandards();

                #region NEP5 METHODS
                if (operation == "totalSupply") return TotalSupply();
                if (operation == "name") return Name();
                if (operation == "symbol") return Symbol();
                if (operation == "decimals") return Decimals();
                if (operation == "transfer")
                {
                    if (args.Length != 3) return false;
                    byte[] from = (byte[])args[0];
                    byte[] to = (byte[])args[1];
                    BigInteger value = (BigInteger)args[2];
                    return Transfer(from, to, value);
                }
                if (operation == "balanceOf")
                {
                    if (args.Length != 1) return 0;
                    byte[] account = (byte[])args[0];
                    return BalanceOf(account);
                }
                #endregion

                #region NEP5.1 METHODS
                if (operation == "allowance")
                {
                    if (args.Length != 2) return false;
                    byte[] from = (byte[])args[0];
                    byte[] to = (byte[])args[1];
                    return Allowance(from, to);
                }

                else if (operation == "approve")
                {
                    if (args.Length != 3) return false;
                    byte[] from = (byte[])args[0];
                    byte[] to = (byte[])args[1];
                    BigInteger value = (BigInteger)args[2];
                    return Approve(from, to, value);
                }

                else if (operation == "transferFrom")
                {
                    if (args.Length != 4) return false;
                    byte[] sender = (byte[])args[0];
                    byte[] from = (byte[])args[1];
                    byte[] to = (byte[])args[2];
                    BigInteger value = (BigInteger)args[3];
                    return TransferFrom(sender, from, to, value);
                }
                #endregion

                #region RED PULSE INFLATION
                if (operation == "inflation") return Inflation();
                if (operation == "inflationRate")
                {
                    if (args.Length != 1) return false;
                    BigInteger rate = (BigInteger)args[0];
                    return InflationRate(rate);
                }
                if (operation == "inflationStartTime")
                {
                    if (args.Length != 1) return false;
                    BigInteger start_time = (BigInteger)args[0];
                    return InflationStartTime(start_time);
                }
                if (operation == "queryInflationRate") return QueryInflationRate();
                if (operation == "queryInflationStartTime") return QueryInflationStartTime();
                #endregion

                #region PROOF OF OWNERSHIP AND CREATION
                if (operation == "storeProof")
                {
                    if (args.Length != 3) return false;
                    byte[] hash = (byte[])args[0];
                    byte[] signer = (byte[])args[1];
                    byte[] owner = (byte[])args[2];
                    return StoreProof(hash, signer, owner);
                }
                if (operation == "transferProof")
                {
                    if (args.Length != 3) return false;
                    byte[] hash = (byte[])args[0];
                    byte[] from = (byte[])args[1];
                    byte[] to = (byte[])args[2];
                    return TransferProof(hash, from, to);
                }
                if (operation == "modifyProof")
                {
                    if (args.Length != 2) return false;
                    byte[] hash = (byte[])args[0];
                    byte[] signer = (byte[])args[1];
                    return ModifyProof(hash, signer);
                }
                if (operation == "proofGetOwner")
                {
                    if (args.Length != 1) return false;
                    byte[] hash = (byte[])args[0];
                    return ProofGetOwner(hash);
                }
                if (operation == "proofGetCreator")
                {
                    if (args.Length != 1) return false;
                    byte[] hash = (byte[])args[0];
                    return ProofGetCreator(hash);
                }
                if (operation == "proofGetSigner")
                {
                    if (args.Length != 1) return false;
                    byte[] hash = (byte[])args[0];
                    return ProofGetSigner(hash);
                }
                if (operation == "proofGetTimestampCreated")
                {
                    if (args.Length != 1) return false;
                    byte[] hash = (byte[])args[0];
                    return ProofGetTimestampCreated(hash);
                }
                if (operation == "proofGetTimestampTransferred")
                {
                    if (args.Length != 1) return false;
                    byte[] hash = (byte[])args[0];
                    return ProofGetTimestampTransferred(hash);
                }
                #endregion

            }
            return false;
        }

        /** Check if supplied address is real */
        private static bool ValidateAddress(byte[] address)
        {
            if (address.Length != 20) return false;
            if (address.AsBigInteger() == 0) return false;
            return true;
        }

        /** Return a storage key for balance of a specific address */
        private static byte[] GetBalanceKey(byte[] address)
        {
            return prefix_balance.Concat(address);
        }

        // initialization parameters, only once. set totalSupply and set full balance on airdrop wallet
        // 初始化参数
        [DisplayName("deploy")]
        public static bool Deploy()
        {
            byte[] total_supply = Storage.Get(Storage.CurrentContext, "totalSupply");
            if (total_supply.Length != 0) return false;
            Storage.Put(Storage.CurrentContext, "totalSupply", totalSupply_initial);
            Storage.Put(Storage.CurrentContext, GetBalanceKey(wallet_red_pulse_platform_airdrop), totalSupply_initial);
            Transferred(null, wallet_red_pulse_platform_airdrop, totalSupply_initial);
            return true;
        }

        // get the total token supply
        // 获取已发行token总量
        [DisplayName("totalSupply")]
        public static BigInteger TotalSupply()
        {
            return Storage.Get(Storage.CurrentContext, "totalSupply").AsBigInteger();
        }

        // function that is always called when someone wants to transfer tokens.
        // 流转token调用
        [DisplayName("transfer")]
        public static bool Transfer(byte[] from, byte[] to, BigInteger value)
        {
            if (value <= 0) return false;
            if (!Runtime.CheckWitness(from)) return false;
            if (!ValidateAddress(to)) return false;

            byte[] key_balance_from = GetBalanceKey(from);
            byte[] key_balance_to = GetBalanceKey(to);

            BigInteger from_value = Storage.Get(Storage.CurrentContext, key_balance_from).AsBigInteger();
            if (from_value < value) return false;
            if (from_value == value)
                Storage.Delete(Storage.CurrentContext, key_balance_from);
            else
                Storage.Put(Storage.CurrentContext, key_balance_from, from_value - value);
            BigInteger to_value = Storage.Get(Storage.CurrentContext, key_balance_to).AsBigInteger();
            Storage.Put(Storage.CurrentContext, key_balance_to, to_value + value);
            Transferred(from, to, value);
            return true;
        }

        // get the account balance of another account with address
        // 根据地址获取token的余额
        [DisplayName("balanceOf")]
        public static BigInteger BalanceOf(byte[] address)
        {
            return Storage.Get(Storage.CurrentContext, GetBalanceKey(address)).AsBigInteger();
        }

        // Gets the amount of tokens allowed by 'from' address to be used by 'to' address
        [DisplayName("allowance")]
        public static BigInteger Allowance(byte[] from, byte[] to)
        {
            if (!ValidateAddress(from)) return 0;
            if (!ValidateAddress(to)) return 0;
            byte[] allowance_key = prefix_allowance.Concat(from).Concat(to);
            return Storage.Get(Storage.CurrentContext, allowance_key).AsBigInteger();
        }

        // Gives approval to the 'to' address to use amount of tokens from the 'from' address
        // This does not guarantee that the funds will be available later to be used by the 'to' address
        // 'From' is the Tx Sender. Each call overwrites the previous value. This matches the ERC20 version
        [DisplayName("approve")]
        public static bool Approve(byte[] from, byte[] to, BigInteger value)
        {
            if (value <= 0) return false;
            if (!Runtime.CheckWitness(from)) return false;
            if (!ValidateAddress(to)) return false;
            if (from == to) return false;

            BigInteger from_value = BalanceOf(from);
            if (from_value < value) return false;

            // overwrite previous value
            byte[] allowance_key = prefix_allowance.Concat(from).Concat(to);
            Storage.Put(Storage.CurrentContext, allowance_key, value);
            OnApproved(from, to, value);
            return true;
        }

        // Transfers tokens from the 'from' address to the 'to' address
        // The Sender must have an allowance from 'From' in order to send it to the 'To'
        // This matches the ERC20 version
        [DisplayName("transferFrom")]
        public static bool TransferFrom(byte[] sender, byte[] from, byte[] to, BigInteger value)
        {
            if (!Runtime.CheckWitness(sender)) return false;
            if (!ValidateAddress(from)) return false;
            if (!ValidateAddress(to)) return false;
            if (value <= 0) return false;

            BigInteger from_value = BalanceOf(from);
            if (from_value < value) return false;
            if (from == to) return true;

            // allowance of [from] to [sender]
            byte[] allowance_key = prefix_allowance.Concat(from).Concat(sender);
            BigInteger allowance = Storage.Get(Storage.CurrentContext, allowance_key).AsBigInteger();
            if (allowance < value) return false;
            byte[] key_balance_from = GetBalanceKey(from);

            if (from_value == value)
                Storage.Delete(Storage.CurrentContext, key_balance_from);
            else
                Storage.Put(Storage.CurrentContext, key_balance_from, from_value - value);

            if (allowance == value)
                Storage.Delete(Storage.CurrentContext, allowance_key);
            else
                Storage.Put(Storage.CurrentContext, allowance_key, allowance - value);

            // Sender sends tokens to 'To'
            BigInteger to_value = BalanceOf(to);
            Storage.Put(Storage.CurrentContext, GetBalanceKey(to), to_value + value);

            Transferred(from, to, value);
            return true;
        }

        [DisplayName("inflation")]
        public static bool Inflation()
        {
            if (!Runtime.CheckWitness(wallet_red_pulse_platform_inflation)) return false;
            BigInteger rate = Storage.Get(Storage.CurrentContext, "inflationRate").AsBigInteger();
            if (rate <= 0) return false;
            BigInteger start_time = Storage.Get(Storage.CurrentContext, "inflationStartTime").AsBigInteger();
            if (start_time == 0) return false;
            uint now = Runtime.Time;
            int time = (int)now - (int)start_time;
            if (time < 0) return false;
            BigInteger total_supply = Storage.Get(Storage.CurrentContext, "totalSupply").AsBigInteger();
            int n = time / 86400 + 1;
            BigInteger day_inflation = 0;
            BigInteger n_day_inflation = 0;
            for (int i = 0; i < n; i++)
            {
                day_inflation = total_supply * rate / 1000000000000;
                n_day_inflation += day_inflation;
                total_supply += day_inflation;
            }
            Storage.Put(Storage.CurrentContext, "totalSupply", total_supply);
            Storage.Put(Storage.CurrentContext, "inflationStartTime", start_time + n * 86400);
            BigInteger owner_token = Storage.Get(Storage.CurrentContext, GetBalanceKey(wallet_red_pulse_platform)).AsBigInteger();
            Storage.Put(Storage.CurrentContext, GetBalanceKey(wallet_red_pulse_platform), owner_token + n_day_inflation);
            Transferred(null, wallet_red_pulse_platform, n_day_inflation);
            return true;
        }

        [DisplayName("inflationRate")]
        public static bool InflationRate(BigInteger rate)
        {
            if (!Runtime.CheckWitness(wallet_red_pulse_platform)) return false;
            if (rate < 0) return false;
            Storage.Put(Storage.CurrentContext, "inflationRate", rate);
            return true;
        }

        [DisplayName("queryInflationRate")]
        public static BigInteger QueryInflationRate()
        {
            return Storage.Get(Storage.CurrentContext, "inflationRate").AsBigInteger();
        }

        [DisplayName("inflationStartTime")]
        public static bool InflationStartTime(BigInteger start_time)
        {
            if (!Runtime.CheckWitness(wallet_red_pulse_platform)) return false;
            if (start_time <= 0) return false;
            BigInteger inflation_start_time = Storage.Get(Storage.CurrentContext, "inflationStartTime").AsBigInteger();
            if (inflation_start_time != 0) return false;
            Storage.Put(Storage.CurrentContext, "inflationStartTime", start_time);
            return true;
        }

        [DisplayName("queryInflationStartTime")]
        public static BigInteger QueryInflationStartTime()
        {
            return Storage.Get(Storage.CurrentContext, "inflationStartTime").AsBigInteger();
        }

        /**
         * Proof of ownership helpers
         */
        [DisplayName("proofGetCreator")]
        public static byte[] ProofGetCreator(byte[] hash)
        {
            if (hash.Length != 64) return null;
            var storage_key = proof_creator_prefix.Concat(hash);
            return Storage.Get(Storage.CurrentContext, storage_key);
        }

        [DisplayName("proofGetOwner")]
        public static byte[] ProofGetOwner(byte[] hash)
        {
            if (hash.Length != 64) return null;
            var storage_key = proof_owner_prefix.Concat(hash);
            return Storage.Get(Storage.CurrentContext, storage_key);
        }

        [DisplayName("proofGetSigner")]
        public static byte[] ProofGetSigner(byte[] hash)
        {
            if (hash.Length != 64) return null;
            var storage_key = proof_signer_prefix.Concat(hash);
            return Storage.Get(Storage.CurrentContext, storage_key);
        }

        [DisplayName("proofGetTimestampCreated")]
        public static byte[] ProofGetTimestampCreated(byte[] hash)
        {
            if (hash.Length != 64) return null;
            var storage_key = proof_timestampCreated_prefix.Concat(hash);
            return Storage.Get(Storage.CurrentContext, storage_key);
        }

        [DisplayName("proofGetTimestampTransferred")]
        public static byte[] ProofGetTimestampTransferred(byte[] hash)
        {
            if (hash.Length != 64) return null;
            var storage_key = proof_timestampTransferred_prefix.Concat(hash);
            return Storage.Get(Storage.CurrentContext, storage_key);
        }

        /**
         * Proof of ownership functions
         */
        [DisplayName("storeProof")]
        public static bool StoreProof(byte[] hash, byte[] signer, byte[] owner)
        {
            // Validate input
            if (!Runtime.CheckWitness(signer)) return false;
            if (!ValidateAddress(owner)) return false;
            if (hash.Length != 64) return false;

            // Check if hash ownership already exists
            byte[] res = ProofGetOwner(hash);
            if (res.Length > 0) return false;

            // Not yet existing. Create now
            Storage.Put(Storage.CurrentContext, proof_owner_prefix.Concat(hash), owner);
            Storage.Put(Storage.CurrentContext, proof_creator_prefix.Concat(hash), owner);
            Storage.Put(Storage.CurrentContext, proof_signer_prefix.Concat(hash), signer);
            Storage.Put(Storage.CurrentContext, proof_timestampCreated_prefix.Concat(hash), Runtime.Time);
            Storage.Put(Storage.CurrentContext, proof_timestampTransferred_prefix.Concat(hash), Runtime.Time);
            ProofCreated(hash, signer, owner);
            return true;
        }

        [DisplayName("transferProof")]
        public static bool TransferProof(byte[] hash, byte[] from, byte[] to)
        {
            // The transaction sender needs to match the from address, else users could transfer
            // ownerships of other users
            if (!Runtime.CheckWitness(from)) return false;

            // Validate input
            if (!ValidateAddress(to)) return false;
            if (hash.Length != 64) return false;

            // Check if user is actually the owner, else fail
            if (ProofGetOwner(hash) != from) return false;

            // Is owner, tranfer now
            Storage.Put(Storage.CurrentContext, proof_owner_prefix.Concat(hash), to);
            Storage.Put(Storage.CurrentContext, proof_timestampTransferred_prefix.Concat(hash), Runtime.Time);
            ProofTransferred(hash, from, to);
            return true;
        }

        [DisplayName("modifyProof")]
        public static bool ModifyProof(byte[] hash, byte[] signer)
        {
            // Only the signer can delete a proof. Make sure that tx sender is the signer
            if (!Runtime.CheckWitness(signer)) return false;

            // Validate input
            if (hash.Length != 64) return false;

            // Check if user is actually the signer (uploader), because only this user can
            // modify it. Else fail
            if (ProofGetSigner(hash) != signer) return false;

            // TX sender is signer, delete now
            Storage.Delete(Storage.CurrentContext, proof_owner_prefix.Concat(hash));
            Storage.Delete(Storage.CurrentContext, proof_creator_prefix.Concat(hash));
            Storage.Delete(Storage.CurrentContext, proof_signer_prefix.Concat(hash));
            Storage.Delete(Storage.CurrentContext, proof_timestampCreated_prefix.Concat(hash));
            Storage.Delete(Storage.CurrentContext, proof_timestampTransferred_prefix.Concat(hash));
            ProofModified(hash, signer);
            return true;
        }
    }
}
