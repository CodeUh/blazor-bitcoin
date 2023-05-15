neo4j-admin server stop
neo4j-admin database restore --from-path "F:\Neo4j\Neo4j Backups\neo4j-2023-05-14T18-09-33.backup" --overwrite-destination=true
neo4j-admin server start