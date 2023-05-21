neo4j-admin server stop
$latest = Get-ChildItem "F:\Neo4j\Neo4j Backups\" | Sort-Object LastWriteTime | Select-Object -last 1
neo4j-admin database restore --from-path $latest.FullName --overwrite-destination=true
neo4j-admin server start