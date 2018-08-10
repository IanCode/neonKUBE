#!/bin/bash
#------------------------------------------------------------------------------
# FILE:         docker-entrypoint.sh
# CONTRIBUTOR:  Jeff Lill
# COPYRIGHT:    Copyright (c) 2016-2018 by neonFORGE, LLC.  All rights reserved.
#
# Loads the Docker host node environment variables before launching TD-Agent
# so these values can be referenced by the TD-Agent configuration file.

# Add the root directory to the PATH.

PATH=${PATH}:/

# Load the Docker host node environment variables.

if [ ! -f /etc/neon/env-host ] ; then
    . log-critical.sh "The [/etc/neon/env-host] file does not exist.  This file must have been generated on the Docker host by [neon-cli] during hive setup and be bound to the container."
    exit 1
fi

. /etc/neon/env-host

# Ensure that [/hostfs/var/log] was mounted and that the [neon-log-host]
# subdirectory exists for the journald position file.

if [ ! -d /hostfs/var/log ] ; then
    . log-critical.sh "The host [/var/log] directory has not been mounted to [/hostfs/var/log]." >&2
    exit 1
fi

mkdir -p /hostfs/var/log/neon-log-host

# Load the neonHIVE constants.

. /neonhive.sh

# Launch TD-Agent.

. log-info.sh "Starting: [neon-log-host]"
td-agent
