"use strict";
const options = require("@bcgov/pipeline-cli").Util.parseArguments();
const changeId = options.pr; //aka pull-request
const version = "1.0.0";
const name = "hets";

const phases = {
  build: {
    namespace: "e0cee6-tools",
    name: `${name}`,
    phase: "build",
    changeId: changeId,
    suffix: `-build-${changeId}`,
    instance: `${name}-build-${changeId}`,
    version: `${version}-${changeId}`,
    tag: `build-${version}-${changeId}`,
    transient: true,
  },
  dev: {
    namespace: "e0cee6-dev",
    name: `${name}`,
    phase: "dev",
    changeId: changeId,
    suffix: `-dev-${changeId}`,
    instance: `${name}-dev-${changeId}`,
    version: `${version}-${changeId}`,
    tag: `dev-${version}-${changeId}`,
    host: `hets-${changeId}-e0cee6-dev.apps.silver.devops.gov.bc.ca`,
    dotnet_env: "Development",
    dbUser: "trdbhetd",
    dbSize: "1Gi",
    transient: true,
    backupVolume: "hets",
    backupVolumeSize: "1Gi",
    verificationVolumeSize: "1Gi",
    db_cpu: "500m",
    db_memory: "512Mi",
    api_cpu: "500m",
    api_memory: "512Mi",
    client_cpu: "100m",
    client_memory: "100Mi",
  },
  test: {
    namespace: "e0cee6-dev",
    name: `${name}`,
    phase: "test",
    changeId: changeId,
    suffix: `-test`,
    instance: `${name}-test`,
    version: `${version}`,
    tag: `test-${version}`,
    host: `hets-e0cee6-test.apps.silver.devops.gov.bc.ca`,
    dbUser: "trdbhett",
    dbSize: "1Gi",
    dotnet_env: "Staging",
    backupVolume: "hets",
    backupVolumeSize: "1Gi",
    verificationVolumeSize: "1Gi",
    db_cpu: "500m",
    db_memory: "512Mi",
    api_cpu: "500m",
    api_memory: "512Mi",
    client_cpu: "100m",
    client_memory: "100Mi",
  },
  uat: {
    namespace: "e0cee6-test",
    name: `${name}`,
    phase: "uat",
    changeId: changeId,
    suffix: `-uat`,
    instance: `${name}-uat`,
    version: `${version}`,
    tag: `test-${version}`,
    host: `hets-e0cee6-uat.apps.silver.devops.gov.bc.ca`,
    dbUser: "trdbhett",
    dbSize: "1Gi",
    dotnet_env: "UAT",
    backupVolume: "hets",
    backupVolumeSize: "1Gi",
    verificationVolumeSize: "1Gi",
    db_cpu: "500m",
    db_memory: "512Mi",
    api_cpu: "500m",
    api_memory: "512Mi",
    client_cpu: "100m",
    client_memory: "100Mi",
  },
  train: {
    namespace: "e0cee6-test",
    name: `${name}`,
    phase: "train",
    changeId: changeId,
    suffix: `-train`,
    instance: `${name}-train`,
    version: `${version}`,
    tag: `train-${version}`,
    host: `hets-e0cee6-train.apps.silver.devops.gov.bc.ca`,
    dbUser: "trdbhett",
    dbSize: "1Gi",
    dotnet_env: "Training",
    backupVolume: "hets",
    backupVolumeSize: "1Gi",
    verificationVolumeSize: "1Gi",
    db_cpu: "500m",
    db_memory: "512Mi",
    api_cpu: "500m",
    api_memory: "512Mi",
    client_cpu: "100m",
    client_memory: "100Mi",
  },
  prod: {
    namespace: "e0cee6-prod",
    name: `${name}`,
    phase: "prod",
    changeId: changeId,
    suffix: `-prod`,
    instance: `${name}-prod`,
    version: `${version}`,
    tag: `prod-${version}`,
    host: `hets-e0cee6-prod.apps.silver.devops.gov.bc.ca`,
    dbUser: "trdbhetp",
    dbSize: "10Gi",
    dotnet_env: "Production",
    backupVolume: "hets",
    backupVolumeSize: "10Gi",
    verificationVolumeSize: "10Gi",
    db_cpu: "1",
    db_memory: "1Gi",
    api_cpu: "1",
    api_memory: "1Gi",
    client_cpu: "200m",
    client_memory: "200Mi",
  },
};

// This callback forces the node process to exit as failure.
process.on("unhandledRejection", (reason) => {
  console.log(reason);
  process.exit(1);
});

module.exports = exports = { phases, options };
