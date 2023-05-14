CREATE CONSTRAINT FOR (block:Block) REQUIRE block.hash IS UNIQUE
CREATE CONSTRAINT FOR  (trans:Transaction) REQUIRE trans.txid IS UNIQUE



