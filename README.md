# Protender (README WIP)
![Build Status](https://github.com/Lemorz56/Protender/actions/workflows/ci.yml/badge.svg?branch=main)
[![Publish](https://github.com/Lemorz56/Protender/actions/workflows/cd.yml/badge.svg)](https://github.com/Lemorz56/Protender/actions/workflows/cd.yml)
---

### About 
This tool can be used when wanting a graphica interface to fill in properties to a protobuf class and then send said class over NATS.

You can select a DLL to load classes from, then select a class that inherits from IMessage, connect to NATS and specify subject and count, then send it.
