// Nodes and relationships for the Bitcoin blockchain data model

// Block node
(:Block {
  hash: "str",
  size: n,
  strippedsize: n,
  weight: n,
  height: n,
  version: n,
  versionHex: "str",
  merkleroot: "str",
  time: n,
  mediantime: n,
  nonce: n,
  bits: "str",
  difficulty: n,
  chainwork: "str",
  nTx: n,
  previousblockhash: "str",
  nextblockhash: "str"
})

// Transaction node
(:Transaction {
  hex: "str",
  txid: "str",
  hash: "str",
  size: n,
  vsize: n,
  weight: n,
  version: n,
  locktime: n,
  blockhash: "str",
  blocktime: n,
  time: n
})

(:Coinbase {
  coinbase: "str",
  sequence: n
})

// Output node
(:Output {
  txid: "str",
  n: n,
  value: n,
})

// Address node
(:Address {
  address: "str"
})

// Relationships
(:Block)-[:CONTAINS]->(:Transaction)
(:Block)-[:COINBASE]->(:Coinbase)
(:Transaction)<-[:IN {asm:"str",hex:"str",reqSigs:n,type="str"}]-(:Output)
(:Transaction)-[:OUT {asm:"str",hex:"str",reqSigs:n,type="str"}]->(:Output)
(:Block)-[:PREV_BLOCK {hash:"str"}]->(:Block)
(:Block)-[:NEXT_BLOCK {hash:"str"}]->(:Block)
(:Transaction)-[:INCLUDED_IN]->(:Block)
(:Output)-[:LOCKED_BY]->(:Address)

//Unique constraints