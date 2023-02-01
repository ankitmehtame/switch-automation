[![Docker Image CI](https://github.com/ankitmehtame/switch-automation/actions/workflows/docker-image.yml/badge.svg)](https://github.com/ankitmehtame/switch-automation/actions/workflows/docker-image.yml)

# Switch Automation
Exposes api for switches over REST

### To build docker image
From the root directory
```
docker build -t switch-automation:latest -t switch-automation:0.n .
```

### To run interactively
From the root directory
```
docker run -it --rm -p 5001:443 -p 5000:80  --name switch-automation switch-automation
```

### To save image locally
From the root directory
```
docker save -o <local path>\switch-automation_0.n.tar switch-automation:latest switch-automation:0.n
```

### To load image
```
docker load --input <path>\switch-automation_0.n.tar
```
