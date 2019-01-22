# Periodic Maintenance 

Once a week or so, we should perform some periodic maintenance described below.

## Docker Packages

The easiest way to to accomplish this is visit the Docker package repo at:

    [https://download.docker.com/linux/ubuntu/dists/bionic/pool/stable/amd64/](https://download.docker.com/linux/ubuntu/dists/bionic/pool/stable/amd64/)

1. Download new Docker CE versions.

2. Rename any new files to the standard format: **docker.ce-VERSION-DISTRO-CODENAME-REPOSITORY_amd64**, where:

  **VERSION** is the *nice* Docker version, like: **18.09.1-ce**  (ignore the part before the colon if present)
  **DISTRO** identifies the Linux distribution, like: **ubuntu**
  **CODENAME** identifies the distribution version, like: **bionic**
  **REPOSITORY** identifies the source repository, like: **stable**

  Example: `docker.ce-18.09.0-ce-ubuntu-bionic-stable-amd64.deb`

3. Upload the files to S3 and **MAKE THEM PUBLIC**: https://s3-us-west-2.amazonaws.com/neonforge/kube/FILENAME.deb
4. Update the headend services (or simulated services for now) to include a Docker version to URL mapping.

## Check for new Kubernetes releases:

1. Check the official release notes: [here](https://github.com/kubernetes/kubernetes/releases)

2. Update the Headend service (currently stubbed)

   a. Start an Ubuntu VM

   b. Configure the Kubernetes package  repo:
```
apt-get update && apt-get install -y apt-transport-https curl
curl - s https://packages.cloud.google.com/apt/doc/apt-key.gpg | apt-key add -
echo "deb https://apt.kubernetes.io/ kubernetes-xenial main" > /etc/apt/sources.list.d/kubernetes.list
```
  c. Run these commands one at a time to discover the latest package releases:
```
apt-cache madison kubeadm
apt-cache madison kubectl
apt-cache madison kubelet
```
  d. Edit the `HeadendClient.cs` file:
    * updating the `latestSupportedVersion` constant.
    * adding or updating the package mappings.

## Check for base Docker image updates:

1. [haproxy](http://haproxy.org)
2. .NET Core