This is a sample application deploy to Kubernetes.  It's a good first application as it has no other components, but it is simple and can be easily modified.

Configuring Kubernetes to host this particular container will teach you the following:

- [Pods](https://kubernetes.io/docs/concepts/workloads/pods/)
- [ReplicaSets](https://kubernetes.io/docs/concepts/workloads/controllers/replicaset/)
- [Kubernetes Deployment Object](https://kubernetes.io/docs/concepts/workloads/controllers/deployment/)
- [Environment Variables](https://kubernetes.io/docs/tasks/inject-data-application/define-environment-variable-container/)
- [Secrets](https://kubernetes.io/docs/concepts/configuration/secret/)
- [Namespaces](https://kubernetes.io/docs/concepts/overview/working-with-objects/namespaces/)
- [Ingress Controllers](https://kubernetes.io/docs/concepts/services-networking/ingress-controllers/)
- [Ingress](https://kubernetes.io/docs/concepts/services-networking/ingress/)

# GitHub Action and Docker Repository

The Docker Container is built using a GitHub action and it is pushed to Docker Hub.  Both a Linux/x86 and Linux/arm64 image are built.  

You can find the docker repository here: https://hub.docker.com/r/octopussamples/randomquotes-k8s/tags

# Forking this repo

Feel free to fork the repo for your own K8s testing and learning.  A few quick notes:

- The primary build is found in `.github/workflows/build.yml`
- The DockerFile is found in `.src/RandomQuotes.Web` - it injects the version from the build into the container itself.
- All the Kubernetes items are found in the `k8s` folder.
   - Base - contains all the basic configuration.
   - Overlays - Kustomize overlays, one for each environment.
   - Charts - The Helm chart.
   - Provision - The core manifest files used to provision a new cluster.  

If you fork this repo you will need to set the following repo secrets:

- `DOCKERHUB_PAT_USERNAME` - your username
- `DOCKERHUB_PAT` - the PAT of your user
- `DOCKERHUB_REPO` - the docker hub repo to store the container - this really isn't a secret, but I made it one so you wouldn't have to modify the build

# Prep Work

If you are using this repository to learn about kubernetes, you can follow the steps and activities below.

The docker image, manifest files, and variables will be provided to you.  You need to provide a k8s cluster, octopus instance, and worker.

## 1. Install / Create K8s Cluster
Setup a K8s cluster in Azure, GCP, or AWS.  Be forewarned, AWS IAM policies often make it the most difficult K8s implementation to use.

Optionally, You can install ONE of the following on a VM or locally for free!

- [docker desktop](https://docs.docker.com/desktop/) - easiest and preferred
  - 🍎 If you are working on a Mac with an Apple chip—Docker Desktop is the easiest option:
    - Run [`softwareupdate --install-rosetta`](https://docs.docker.com/desktop/install/mac-install/#system-requirements)
    - Enable [Kubernetes](https://docs.docker.com/desktop/kubernetes/#install-and-turn-on-kubernetes)
    - Confirm `Use Virtualization framework` is enabled in Docker Desktop → General → Settings
- [rancher desktop](https://docs.rancherdesktop.io/getting-started/installation) -> please note you will not have to install docker for this to work.
- [minikube](https://minikube.sigs.k8s.io/docs/start/)

## 2. Configure K8s
Open up a command prompt or terminal.  Change the current directory in the terminal to the `k8s/provision` folder in this repo.
- Run the following commands:
    - Create all the namespaces: `kubectl apply -f namespaces.yaml`
    - Install the NGINX Ingress Controller: `kubectl apply -f https://raw.githubusercontent.com/kubernetes/ingress-nginx/controller-v1.9.5/deploy/static/provider/cloud/deploy.yaml`

## 3. Pre-Configure Octopus
Using your Octopus Deploy cloud instance of choice do the following:

- Create a worker pool name "Local K8s Worker Pool".
- Ensure you have the following environments: "Development", "Test", "Staging", "Production"
- Install the kubernetes agent on your K8s cluster.
- Go to Library -> Feeds
    - Add a docker hub feed
    - Provide your username and PAT or a service account username and PAT otherwise you won't be able to create releases.
- Go to Infrastructure -> Targets.  Add the kubernetes cluster using the agent.  
    - Use the Local K8s Worker Pool from earlier.
    - Assign it to all four environments from earlier.
    - Use the role `local-k8s`.
- Go to Library -> Git Credentials.
    - Add a new GitHub PAT token for your user.  
        - The PAT will need explict access to OctopusSamples.  
        - Create a fine-grained key
        - Set the resource owner to OctopusSamples
        - Select Public Repositories (read-only) as the option.
    - Username will be your username.

## 4. Configure your hosts file.
Go to your hosts file (if running the K8s cluster on Windows locally) and add the following entries.  The nginx ingress controller uses host headers for all routing.  Doing this will allow you to easily access the application running on your k8s cluster.

```
127.0.0.1       randomquotes.local
127.0.0.1       randomquotesdev.local
127.0.0.1       randomquotestest.local
127.0.0.1       randomquotesstaging.local
127.0.0.1       randomquotesprod.local
```

# Activities

All the activities below will be done to help teach K8s deployments.

## 1. First Activity - Basic Deployment 

In the first activity we will do a standard manifest file deployment to the default namespace in kubernetes using `kubectl apply`.

The goal of this activity is to expose you to a simple manifest file and what the experience is like for a bare-bones, no automation experience.

These instructions will deploy the following to the default namespace.  

- Secret
- Deployment (Image)
- ClusterIp Service
- Ingress Rule

To perform the deployment do the following:
- Go to https://hub.docker.com/r/octopussamples (or your repo)/randomquotes-k8s/tags and find the latest version tag (0.1.3 for example).  Update the `image` entry in the randomquotes-deployment.yaml file.
- Open up a command prompt or terminal.  Change the current directory in the terminal to the `k8s/base` folder in this repo. 
- Run `kubectl apply -f randomquotes-secrets.yaml`
- Run `kubectl apply -f randomquotes-deployment.yaml`

It might take a moment for the deployment to finish.  I like to check the status of the pods.  Run `kubectl get pods` until the randomquotes pod shows up as healthy.

Once the deployment is finished go to http://randomquotes.local.

## 2. Second Activity - Leveraging Kustomize

In the second activity we will deploy to each of the environment namespaces using kustomize and overlays.

In the previous activity we deployed to the default namespace.  In the real-world, we'd want to do some environmental progression and testing before pushing up to production.  Rather than have a manifest file per environment, we will have a kustomize overlay per environment.  The following items are changed with each kustomize file:

- The image version
- The secret value
- The ingress rule

If we were using ArgoCD or some other similar tool we could use these kustomize overlays with no additional configuration changes.  

We are going to deploy to all four namespaces.  The instructions are the same, so repeat the following for each environment.

- Go to https://hub.docker.com/r/octopussamples/randomquotes-k8s/tags and find the latest version tag (0.1.3 for example).  Go to k8s/overlays/[environment]/kustomization.yaml file.  Update the newtag entry with the latest version.
- Open up a command prompt or terminal.  Change the current directory in the terminal to the `k8s/overlays/[environment]/` folder in this repo. 
- Run `kubectl apply -k ./`
- To check the pods run `kubectl get pods -n [environment namespace name]`
- Once the deployment is over go to http://randomquotes[environment name].local
- Assuming everything is working, repeat the following steps for each environment.

## 3. Third Activity - Using Octopus Deploy

In the third activity we will configure Octopus Deploy to deploy our application to k8s using the manifest files.

In this example, we will put the kustomize overlays aside and instead use Octopus' raw yaml steps + structured variable configuration.

- Create a new project called `Random Quotes Manifest Files`
- Add the following variables:
    - Name: spec:rules:0:host
        - Values: `randomquotesdev.local` with Environment scope: `Development`
        - Values: `randomquotestest.local` with Environment scope: `Test`
        - Values: `randomquotesstaging.local` with Environment scope: `Staging`
        - Values: `randomquotesprod.local` with Environment scope: `Production`
        - Type: Text
    - Name: spec:template:spec:containers:0:image
        - Value: octopussamples/randomquotes-k8s:#{Octopus.Action.Package[randomquotes-k8s].PackageVersion}
        - Type: Text
    - Name: stringData:homepageDisplay
        - Value: [Your Choice]
        - Type: Sensitive
- Go to the deployment process
    - Add a DEPLOY RAW KUBERNETES YAML
        - Name: Create Random Quotes Secret
        - Worker Pool: Since you are running the agent, this shouldn't matter
        - Role: Use the role from your k8s cluster
        - YAML Source: Git Repository
        - Git Credentials: Use the git credentials from the library
        - Repository URL: https://github.com/[YOUR USER OR ORG]/RandomQuotes-K8s.git 
        - Branch Settings: main
        - Paths: k8s/base/randomquotes-secrets.yaml
        - Structured Configuration Values: Check the `Enable Structured Configuration Variables` checkbox
        - Namespace: #{Octopus.Environment.Name | ToLower}
    - Add a DEPLOY RAW KUBERNETES YAML
        - Name: Deploy Random Quotes
        - Worker Pool: Since you are running the agent, this shouldn't matter
        - Role: Use the role from your k8s cluster
        - YAML Source: Git Repository
        - Git Credentials: Use the git credentials from the library
        - Repository URL: https://github.com/[YOUR USER OR ORG]/RandomQuotes-K8s.git 
        - Branch Settings: main
        - Paths: k8s/base/randomquotes-deployment.yaml
        - Structured Configuration Valures: Check the `Enable Structured Configuration Variables` checkbox
        - Referenced Packages: 
            - Package Feed: DockerHub
            - PackageId: [octopussamples | REPLACE octopussamples with your user]/randomquotes-k8s
            - Name: randomquotes-k8s
        - Namespace: #{Octopus.Environment.Name | ToLower}
    - Save the deployment process
- Create a release and deploy it to dev.
- Test by going to randomquotesdev.local.
- Promote the release through each environment.  Test along the way.

## 4. Fourth Activity - ArgoCD - Optional!

If you wish to learn ArgoCD, you can perform these steps.

First install ArgoCD on the cluster.

- Install ArgoCD
    - Run `kubectl create namespace argocd`
    - Run `kubectl apply -n argocd -f https://raw.githubusercontent.com/argoproj/argo-cd/stable/manifests/install.yaml`
- To access ArgoCD UI
    - Run `kubectl port-forward svc/argocd-server -n argocd 8080:443`
    - **Important** The port forwarding will only work while that window is open.
    - If you want to, you can mess with ingress rules, but this is the quick and dirty approach to getting going.
    - To login
        - Username is admin
        - Run `kubectl get secret argocd-initial-admin-secret -o jsonpath='{.data.password}' --namespace argocd` to get the password.  
        - Please note it is base64, which you will need to decode.  You can do that via an online editor, or PowerShell `[System.Text.Encoding]::UTF8.GetString([System.Convert]::FromBase64String("blahblahblah"))`

- Configure first application 
    - Click the `New App` button
    - Application Name: `randomquotes-dev`
    - Project Name: `default`
    - Repository URL: `https://github.com/OctopusSamples/RandomQuotes-K8s.git`    
    - Paths: `k8s/overlays/dev`
    - Cluster Url: `https://kubernetes.default.svc`
    - Namespace: `development`
    - Click the `Create` button
    - Click the `Sync` button
    - Go to randomquotesdev.local to verify it is running
- Repeat the above section for test, staging, and production
# My super cool Octopus deployment pipeline
