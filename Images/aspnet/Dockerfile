#------------------------------------------------------------------------------
# FILE:         Dockerfile
# CONTRIBUTOR:  Jeff Lill
# COPYRIGHT:    Copyright (c) 2016-2018 by neonFORGE, LLC.  All rights reserved.
#
# Base ASP.NET Core image.
#
# ARGUMENTS:
#
#   VERSION         - The ASP.NET Core image tag (e.g. "2.1.3")

ARG         VERSION
FROM        microsoft/dotnet:${VERSION}-aspnetcore-runtime-alpine3.7
MAINTAINER  jeff@lilltek.com
STOPSIGNAL  SIGTERM

# Opt-out of .NET Core telemetry.

ENV DOTNET_CLI_TELEMETRY_OPTOUT=1

# Add a file to tweak the DNS resolver's [/etc/resolve.conf] settings.

COPY resolv.conf.head.sh    /etc/resolv.conf.head

# Copy common scripts to the root.

COPY _common/*.sh           /

# Install the [tini] INIT manager.

RUN apk add --no-cache tini ca-certificates