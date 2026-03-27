# RandomQuotes-K8s

This is a sample .NET app deployed via a CI/CD pipeline I built as part of the Octopus Deploy technical chat.

## The pipeline

```
GitHub → GitHub Actions → Docker Hub → Octopus Deploy → AKS → 🎉
```

The CI side is handled by GitHub Actions. When I push to `main` (specifically changes to `src/` or `.github/`), it kicks off a build using GitVersion for semantic versioning, builds a multi-platform Docker image, and pushes it to Docker Hub with both a version tag and `latest`.

Octopus handles the CD side. There's a Kubernetes agent running in my AKS cluster that polls Octopus for work. When I create a release and deploy, Octopus sends the manifests to the agent, and the agent applies them to the cluster. Right now release creation is manual, but you could automate it with external feed triggers.

The app runs on Azure Kubernetes Service — single node, `Standard_D2s_v3` VM, with a LoadBalancer service so it gets a public IP.

## Things that broke (a partial list)

I started on Docker Desktop on my M-series Mac, and the .NET app immediately crashed with assembly load errors. ARM64 incompatibility. Even the official sample image failed. Off to a great start!

I pivoted to AKS (which apparently runs AMD64) and that solved it. 

Then Azure told me my subscription didn't have quota for `Standard_B2s`. Tried `standard_b2pls_v2`. Also no. Ran `az vm list-usage` to find a VM family that actually had quota allocated. Third time's the charm with `Standard_D2s_v3`.

At one point a failed cluster creation left a zombie resource behind. `az aks show` said "Failed." Tried to delete it, got "NotFound." I eventually nuked the whole resource group and started fresh.

Apparently Octopus has this lifecycle progression feature that required me to deploy to `dev_paul` before `azure-prod`. But `dev_paul` was my broken local cluster. So I fixed it by creating an explicit lifecycle phase with only `azure-prod`.

I manually patched the Kubernetes service to `LoadBalancer` to get a public IP. Next Octopus deploy overwrote it back to `NodePort` because that's what was in the manifest. Lesson learned: the manifest is the source of truth, not your `kubectl patch` commands.

And my favorite: I spent a while wondering why code changes weren't showing up. Turns out Octopus was deploying `octopussamples/randomquotes-k8s`, not my image. The build was working fine. The deploy was just ignoring it entirely.

## How it works now

Push code to `main`. GitHub Actions builds and pushes the image to `paulplayground/paul-randomquotes-k8s`. Create a release in Octopus. Deploy to `azure-prod`. App is live at the LoadBalancer IP. Hurray.

## Local setup

If you want to reproduce this, you'll need an Azure account with AKS quota (good luck), a Docker Hub account, an Octopus Cloud account (free tier works), and the `az`, `kubectl`, and `helm` CLI tools installed.

## What I'd add for production

The obvious stuff: auto-trigger releases when new images hit Docker Hub, multiple environments with proper promotion gates, Kubernetes health checks, secrets management, Slack notifications on deploy success/failure. Also a budget alert, because I forgot to turn off AKS the first time.

---

Built for the Octopus Deploy SE interview process. If you're reading this, hi! 👋