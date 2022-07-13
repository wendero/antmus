# Antmus
Antmus (ANoTher MockUp Server) is an API Mock Server that can **Record** calls and reply them back as a **Mock**.

It runs in a docker container in two different stages. First it needs to record the calls. In order to do that, it needs to run in [Recorder Mode](##Recorder-Mode). Once it's running all calls must be sent. The second stage is to stop de container and start it once again in [Mock Mode](##Mock-Mode) so all previous mocks will be replied for the respective client call.

## How it works?
Antmus will record request-response pairs for every call it receives. All mocks are [idempotent](https://en.wikipedia.org/wiki/Idempotence) which means that for the same input (request) it will always reply with the same output (response). 

A mock file stores the request method and a hash that is calculated from the unique combination of headers, url and body. It also stores the response value with its [http code](https://en.wikipedia.org/wiki/List_of_HTTP_status_codes), serialized body (hexadecimal) and headers.

> For situations where the same input must provide a different output it's recommended to use a [special header](##Headers) with different values in order to change the request hash.

> Example: 
> - GET /user
> - PUT /user
> - GET /user *<= this response might be different*

When running in [Mock Mode](##Mock-Mode) all the recorded mocks will be loaded into a dictionary and the response will be parsed in runtime based on the request method/hash combination and replied.

## Configuration

The following environment variables can be used to customize the Antmus' behavior:
| Variable | Description | Example |
| -------- | ----------- | ------- |
| Redirect | Defines the target Api to be cloned when running in [Recorder Mode](##Recorder-Mode) | `-e Redirect="http://host.docker.internal:<port>"` |
| Mode | Changes Antmus running mode (Mock or Recorder) | `-e Mode="Mock"` |

## Recorder Mode
The easiest way to run Antmus in Recorder Mode is by running docker command:
```
docker run -it -e Redirect="<target api url>" --name antmus --rm -p5000:5000 -v <host absolute path>:/mocks antmus
```

> The recorder mode is set as default so the mode parameter doesn't need to be set. 

### Target Api running in the Docker Host instance
In order to make it work we need to add the docker host IP into the container. So add the follow parameter in your docker run command: `-e Redirect="http://host.docker.internal:<port>" --add-host=host.docker.internal:<host ip address>`

> Firewall rules must be reviewed in the host to allow inbound request target this API.

## Mock Mode

To run in Mock Mode the container must run with Mode environment variable:
```
docker run -itd -e Redirect="<target api url>" --name antmus --rm -p5000:5000 antmus
```

## Headers
Headers starting with the prefix `Antmus-` can be used to add idempotency to request with the same endpoint. For instance, a header `Antmus-Test-Sequence: <number>` can be used to make different responses for the same request once the header itself will be used as input ensuring that the mock is stateless. 

## Antmus API
When running in recorder mode Antmus has an API accesses by the `/_antmus` endpoint. Check the `Antmus.postman_collection.json` for examples on how to create mocks using Antmus API.

## Mock Types
Antmus has two mock types:
- Default: a file in the format `Method_RequestHash.json` is created and Antmus reads the response On-Demand (on each call only the file needed will be read) based on the request auto-calculated hash.
- Custom: a file with any name with extension `.custom.json`. This mock has a different behavior once it can use filters based on path, headers and content, to define the mock matching. 
> Warning: Custom Mocks are loaded in memory during startup. Avoid huge request/responses.
