package cluster

import (
	"github.com/loopieio/cadence-proxy/cmd/cadenceproxy/cadenceerrors"
	"github.com/loopieio/cadence-proxy/cmd/cadenceproxy/messages"
	"github.com/loopieio/cadence-proxy/cmd/cadenceproxy/messages/base"
)

type (

	// DomainUpdateReply is a ProxyReply of MessageType
	// DomainUpdateReply.  It holds a reference to a ProxyReply in memory
	DomainUpdateReply struct {
		*base.ProxyReply
	}
)

// NewDomainUpdateReply is the default constructor for
// a DomainUpdateReply
//
// returns *DomainUpdateReply -> a pointer to a newly initialized
// DomainUpdateReply in memory
func NewDomainUpdateReply() *DomainUpdateReply {
	reply := new(DomainUpdateReply)
	reply.ProxyReply = base.NewProxyReply()
	reply.Type = messages.DomainUpdateReply

	return reply
}

// -------------------------------------------------------------------------
// IProxyMessage interface methods for implementing the IProxyMessage interface

// Clone inherits docs from ProxyMessage.Clone()
func (reply *DomainUpdateReply) Clone() base.IProxyMessage {
	domainUpdateReply := NewDomainUpdateReply()
	var messageClone base.IProxyMessage = domainUpdateReply
	reply.CopyTo(messageClone)

	return messageClone
}

// CopyTo inherits docs from ProxyMessage.CopyTo()
func (reply *DomainUpdateReply) CopyTo(target base.IProxyMessage) {
	reply.ProxyReply.CopyTo(target)
}

// SetProxyMessage inherits docs from ProxyMessage.SetProxyMessage()
func (reply *DomainUpdateReply) SetProxyMessage(value *base.ProxyMessage) {
	reply.ProxyMessage.SetProxyMessage(value)
}

// GetProxyMessage inherits docs from ProxyMessage.GetProxyMessage()
func (reply *DomainUpdateReply) GetProxyMessage() *base.ProxyMessage {
	return reply.ProxyMessage.GetProxyMessage()
}

// GetRequestID inherits docs from ProxyMessage.GetRequestID()
func (reply *DomainUpdateReply) GetRequestID() int64 {
	return reply.ProxyMessage.GetRequestID()
}

// SetRequestID inherits docs from ProxyMessage.SetRequestID()
func (reply *DomainUpdateReply) SetRequestID(value int64) {
	reply.ProxyMessage.SetRequestID(value)
}

// -------------------------------------------------------------------------
// IProxyReply interface methods for implementing the IProxyReply interface

// GetError inherits docs from ProxyReply.GetError()
func (reply *DomainUpdateReply) GetError() *cadenceerrors.CadenceError {
	return reply.ProxyReply.GetError()
}

// SetError inherits docs from ProxyReply.SetError()
func (reply *DomainUpdateReply) SetError(value *cadenceerrors.CadenceError) {
	reply.ProxyReply.SetError(value)
}
