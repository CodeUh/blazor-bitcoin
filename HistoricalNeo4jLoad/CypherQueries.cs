using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HistoricalNeo4jLoad
{
    public static class CypherQueries
    {
        public static string LoadBlockCypher => @"
            UNWIND $blocks AS blockData
            MERGE (b:Block {Hash: blockData.Hash})
            SET b += apoc.map.clean(blockData, [""Tx""], [])
            WITH b, blockData.Tx AS transactions
            UNWIND transactions AS transactionData
            MERGE (t:Transaction {TxId: transactionData.TxId})
            SET t += apoc.map.clean(transactionData, [""Vout"", ""Vin""], [])
            MERGE (b)-[:CONTAINS]->(t)

            WITH t, transactionData.Vout AS outputs, transactionData
            UNWIND outputs AS outputData
            MERGE (o:Output {TxId: t.TxId, N: outputData.N})
            SET o.Value = outputData.Value
            MERGE (t)-[:OUT]->(o)

            WITH t, o, outputData.ScriptPubKey.Addresses AS addresses, outputData.ScriptPubKey.Address AS addr, transactionData
            MERGE (ad:Address {Address: addr})
            MERGE (o)-[:LOCKED_BY]->(ad)

            WITH t, o, addresses, transactionData
            UNWIND addresses AS address
            MERGE (a:Address {Address: address})
            MERGE (o)-[:LOCKED_BY]->(a)

            WITH t, transactionData.Vin AS inputs
            UNWIND inputs AS inputData
            MERGE (o2:Output {TxId: inputData.TxId, N: inputData.Vout})
            ON CREATE SET o2.Sequence = inputData.Sequence, o2.TxId = inputData.TxId, o2.N = inputData.Vout

            WITH t, o2, inputData
            WHERE inputData.Coinbase IS NULL
            MERGE (t)<-[:IN]-(o2)

            WITH t, inputData
            WHERE inputData.Coinbase IS NOT NULL
            MERGE (cb:Coinbase {Sequence: inputData.Sequence, Coinbase: inputData.Coinbase})
            MERGE (b)-[:COINBASE]->(cb)
            MERGE (cb)-[:IN]->(t)

        ";
        public static string ShowConstraints => @"
            SHOW CONSTRAINTS
        ";
        public static List<string> CreateConstraints => new List<string>() 
        {
            "CREATE CONSTRAINT unique_block_hash IF NOT EXISTS FOR (b:Block) REQUIRE b.Hash IS UNIQUE",
            "CREATE CONSTRAINT unique_transaction_txid IF NOT EXISTS FOR (t:Transaction) REQUIRE t.TxId IS UNIQUE",
            "CREATE CONSTRAINT unique_output_txid_n IF NOT EXISTS FOR (o:Output) REQUIRE (o.TxId, o.N) IS NODE KEY",
            "CREATE CONSTRAINT unique_address_address IF NOT EXISTS FOR (a:Address) REQUIRE a.Address IS UNIQUE"
        };

    }
}
