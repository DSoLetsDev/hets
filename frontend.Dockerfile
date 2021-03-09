FROM e0cee6-tools/client
# Dockerfile for the application front end

# Switch to root for package installs
USER 0

# compile the client
WORKDIR /opt/app-root/

# copy the full source for the client
COPY Client /opt/app-root/

ENV NVM_DIR /usr/local/nvm

RUN . $NVM_DIR/nvm.sh && \
    nvm use v8.9.1

# disable the production switch 	
RUN npm config set -g production false	
RUN npm install

# build the client app   
RUN /bin/bash -c './node_modules/.bin/gulp --production --commit=$OPENSHIFT_BUILD_COMMIT'   

# enable production switch 	
RUN npm config set -g production true	
RUN npm install

# modify 
RUN chown -R 1001:0 /opt/app-root/ && fix-permissions /opt/app-root/

USER 1001