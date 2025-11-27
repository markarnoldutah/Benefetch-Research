@description('Name of the existing Cosmos DB account')
param accountName string = 'bf-cosmos-dev-westus3'

@description('Name of the SQL database to use')
param dbName string = 'bfdb'

// Reference existing Cosmos DB account
resource cosmosDbAccount 'Microsoft.DocumentDB/databaseAccounts@2023-04-15' existing = {
  name: accountName
}

// Create (or ensure) SQL database (no throughput settings in serverless)
resource bfSqlDb 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases@2023-04-15' = {
  name: dbName
  parent: cosmosDbAccount
  properties: {
    resource: {
      id: dbName
    }
    // options left empty for serverless
    options: {}
  }
}

//
// PATIENTS CONTAINER (serverless-compatible)
//
resource patientsContainer 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2023-04-15' = {
  name: 'patients'
  parent: bfSqlDb
  properties: {
    resource: {
      id: 'patients'
      partitionKey: {
        paths: [
          '/patientId'
        ]
        kind: 'Hash'
        version: 2
      }
      indexingPolicy: {
        indexingMode: 'consistent'
        automatic: false
        includedPaths: [
          { path: '/patientId/?' }
          { path: '/tenantId/?' }
          { path: '/practiceId/?' }
          { path: '/firstName/?' }
          { path: '/lastName/?' }
          { path: '/coverageEnrollments/*/memberId/*' }
        ]
        excludedPaths: [
          { path: '/*' }
        ]
      }
    }
    // no options.autoscaleSettings or throughput in serverless
    options: {}
  }
}

//
// ENCOUNTERS CONTAINER (serverless-compatible)
//
resource encountersContainer 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2023-04-15' = {
  name: 'encounters'
  parent: bfSqlDb
  properties: {
    resource: {
      id: 'encounters'
      partitionKey: {
        paths: [
          '/patientId'
        ]
        kind: 'Hash'
        version: 2
      }
      indexingPolicy: {
        indexingMode: 'consistent'
        automatic: false
        includedPaths: [
          { path: '/encounterId/?' }
          { path: '/patientId/?' }
          { path: '/tenantId/?' }
          { path: '/practiceId/?' }
          { path: '/visitDate/?' }
          { path: '/visitType/?' }
          { path: '/coverageDecision/primaryCoverageEnrollmentId/?' }
          { path: '/eligibilityChecks/*/status/*' }
          { path: '/eligibilityChecks/*/payerId/*' }
        ]
        excludedPaths: [
          { path: '/*' }
        ]
        compositeIndexes: [
          [
            { path: '/tenantId',   order: 'ascending' }
            { path: '/practiceId', order: 'ascending' }
            { path: '/visitDate',  order: 'descending' }
          ]
        ]
      }
    }
    options: {}
  }
}

//
// PAYERS CONTAINER (serverless-compatible)
//
resource payersContainer 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2023-04-15' = {
  name: 'payers'
  parent: bfSqlDb
  properties: {
    resource: {
      id: 'payers'
      partitionKey: {
        paths: [
          '/payerId'
        ]
        kind: 'Hash'
        version: 2
      }
      indexingPolicy: {
        indexingMode: 'consistent'
        automatic: false
        includedPaths: [
          { path: '/payerId/?' }
          { path: '/tenantId/?' }
          { path: '/name/?' }
          { path: '/planType/?' }
          { path: '/availityPayerCode/?' }
          { path: '/x12PayerId/?' }
        ]
        excludedPaths: [
          { path: '/*' }
        ]
      }
    }
    options: {}
  }
}
