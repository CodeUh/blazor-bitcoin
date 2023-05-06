namespace BlazorBitcoin.Shared.Models
{
    using System.Collections.Generic;
    using System.Text.Json.Serialization;
    using BlazorBitcoin.Shared.Helpers;
    using Newtonsoft.Json;

    public class BlockResponse
    {
        [JsonProperty("result")]
        [JsonPropertyName("result")]
        public BlockResult Result { get; set; } = new BlockResult();

        [JsonProperty("error")]
        [JsonPropertyName("error")]
        public object Error { get; set; } = new object();

        [JsonProperty("id")]
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;
    }

    public class BlockResult
    {
        [JsonProperty("hash")]
        [JsonPropertyName("hash")]
        public string Hash { get; set; } = string.Empty;

        [JsonProperty("confirmations")]
        [JsonPropertyName("confirmations")]
        public int Confirmations { get; set; } = 0;

        [JsonProperty("height")]
        [JsonPropertyName("height")]
        public int Height { get; set; } = 0;

        [JsonProperty("version")]
        [JsonPropertyName("version")]
        public int Version { get; set; } = 0;

        [JsonProperty("versionHex")]
        [JsonPropertyName("versionHex")]
        public string VersionHex { get; set; } = string.Empty;

        [JsonProperty("merkleroot")]
        [JsonPropertyName("merkleroot")]
        public string MerkleRoot { get; set; } = string.Empty;

        [JsonProperty("time")]
        [JsonPropertyName("time")]
        public int Time { get; set; } = 0;

        public string DisplayTime => UnixTimeHelper.UnixTimeToDateTime(Time).ToString("yyyy-MM-dd HH:mm:ss");
        public DateTime TimeDateTime => UnixTimeHelper.UnixTimeToDateTime(Time);

        [JsonProperty("mediantime")]
        [JsonPropertyName("mediantime")]
        public int MedianTime { get; set; } = 0;

        [JsonProperty("nonce")]
        [JsonPropertyName("nonce")]
        public long Nonce { get; set; } = 0;

        [JsonProperty("bits")]
        [JsonPropertyName("bits")]
        public string Bits { get; set; } = string.Empty;

        [JsonProperty("difficulty")]
        [JsonPropertyName("difficulty")]
        public double Difficulty { get; set; } = 0;

        [JsonProperty("chainwork")]
        [JsonPropertyName("chainwork")]
        public string ChainWork { get; set; } = string.Empty;

        [JsonProperty("nTx")]
        [JsonPropertyName("nTx")]
        public int NTx { get; set; } = 0;

        [JsonProperty("previousblockhash")]
        [JsonPropertyName("previousblockhash")]
        public string PreviousBlockHash { get; set; } = string.Empty;

        [JsonProperty("nextblockhash")]
        [JsonPropertyName("nextblockhash")]
        public string NextBlockHash { get; set; } = string.Empty;

        [JsonProperty("strippedsize")]
        [JsonPropertyName("strippedsize")]
        public int StrippedSize { get; set; } = 0;

        [JsonProperty("size")]
        [JsonPropertyName("size")]
        public int Size { get; set; } = 0;

        [JsonProperty("weight")]
        [JsonPropertyName("weight")]
        public int Weight { get; set; } = 0;

        //TODO: think about this...
        //[JsonProperty("tx")]
        //[JsonPropertyName("tx")]
        //public List<Transaction> Transactions { get; set; } = new List<Transaction>();
    }

    public class Transaction
    {
        [JsonProperty("txid")]
        [JsonPropertyName("txid")]
        public string TxId { get; set; } = string.Empty;

        [JsonProperty("hash")]
        [JsonPropertyName("hash")]
        public string Hash { get; set; } = string.Empty;

        [JsonProperty("version")]
        [JsonPropertyName("version")]
        public int Version { get; set; } = 0;

        [JsonProperty("size")]
        [JsonPropertyName("size")]
        public int Size { get; set; } = 0;

        [JsonProperty("vsize")]
        [JsonPropertyName("vsize")]
        public int VSize { get; set; } = 0;

        [JsonProperty("weight")]
        [JsonPropertyName("weight")]
        public int Weight { get; set; } = 0;

        [JsonProperty("locktime")]
        [JsonPropertyName("locktime")]
        public int LockTime { get; set; } = 0;

        [JsonProperty("vin")]
        [JsonPropertyName("vin")]
        public List<Vin> Vin { get; set; } = new List<Vin>();

        [JsonProperty("vout")]
        [JsonPropertyName("vout")]
        public List<Vout> Vout { get; set; } = new List<Vout>();

        [JsonProperty("hex")]
        [JsonPropertyName("hex")]
        public string Hex { get; set; } = string.Empty;
    }

    public class Vin
    {
        [JsonProperty("coinbase")]
        [JsonPropertyName("coinbase")]
        public string Coinbase { get; set; } = string.Empty;

        [JsonProperty("sequence")]
        [JsonPropertyName("sequence")]
        public long Sequence { get; set; } = 0;

        [JsonProperty("vout")]
        [JsonPropertyName("vout")]
        public int Vout { get; set; } = 0;

        [JsonProperty("txid")]
        [JsonPropertyName("txid")]
        public string TxId { get; set; } = string.Empty;

        [JsonProperty("scriptSig")]
        [JsonPropertyName("scriptSig")]
        public ScriptSig ScriptSig { get; set; } = new ScriptSig();
    }

    public class ScriptSig
    {
        [JsonProperty("asm")]
        [JsonPropertyName("asm")]
        public string Asm { get; set; } = string.Empty;

        [JsonProperty("hex")]
        [JsonPropertyName("hex")]
        public string Hex { get; set; } = string.Empty;
    }

    public class Vout
    {
        [JsonProperty("value")]
        [JsonPropertyName("value")]
        public double Value { get; set; } = 0;

        [JsonProperty("n")]
        [JsonPropertyName("n")]
        public int N { get; set; } = 0;

        [JsonProperty("scriptPubKey")]
        [JsonPropertyName("scriptPubKey")]
        public ScriptPubKey ScriptPubKey { get; set; } = new ScriptPubKey();
    }

    public class ScriptPubKey
    {
        [JsonProperty("asm")]
        [JsonPropertyName("asm")]
        public string Asm { get; set; } = string.Empty;

        [JsonProperty("hex")]
        [JsonPropertyName("hex")]
        public string Hex { get; set; } = string.Empty;

        [JsonProperty("type")]
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;
    }
}


