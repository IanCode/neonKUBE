package base

import (
	"github.com/loopieio/cadence-proxy/cmd/cadenceproxy/cadenceerrors"
)

type (

	// ProxyReply is a IProxyMessage type used for replying to
	// a proxy message request.  It implements the IProxyMessage interface
	// and holds a reference to a ProxyMessage
	ProxyReply struct {

		// ProxyMessage is a reference to a ProxyMessage type
		*ProxyMessage
	}

	// IProxyReply is an interface for all ProxyReply message types.
	// It allows any message type that implements the IProxyReply interface
	// to use any methods defined.  The primary use of this interface is to
	// allow message types that implement it to get and set their nested ProxyReply
	IProxyReply interface {
		GetError() *cadenceerrors.CadenceError
		SetError(value *cadenceerrors.CadenceError)
		GetRequestID() int64
		SetRequestID(value int64)
	}
)

// NewProxyReply is the default constructor for ProxyReply.
// It creates a new ProxyReply in memory and then creates and sets
// a reference to a new ProxyMessage in the ProxyReply.
//
// returns *ProxyReply -> a pointer to a new ProxyReply in memory
func NewProxyReply() *ProxyReply {
	reply := new(ProxyReply)
	reply.ProxyMessage = NewProxyMessage()
	return reply
}

// -------------------------------------------------------------------------
// IProxyMessage interface methods for implementing the IProxyMessage interface

// Clone inherits docs from ProxyMessage.Clone()
func (reply *ProxyReply) Clone() IProxyMessage {
	proxyReply := NewProxyReply()
	var messageClone IProxyMessage = proxyReply
	reply.CopyTo(messageClone)
	return messageClone
}

// CopyTo inherits docs from ProxyMessage.CopyTo()
func (reply *ProxyReply) CopyTo(target IProxyMessage) {
	reply.ProxyMessage.CopyTo(target)
	if v, ok := target.(IProxyReply); ok {
		v.SetError(reply.GetError())
	}
}

// SetProxyMessage inherits docs from ProxyMessage.SetProxyMessage()
func (reply *ProxyReply) SetProxyMessage(value *ProxyMessage) {
	reply.ProxyMessage.SetProxyMessage(value)
}

// GetProxyMessage inherits docs from ProxyMessage.GetProxyMessage()
func (reply *ProxyReply) GetProxyMessage() *ProxyMessage {
	return reply.ProxyMessage.GetProxyMessage()
}

// GetRequestID inherits docs from ProxyMessage.GetRequestID()
func (reply *ProxyReply) GetRequestID() int64 {
	return reply.ProxyMessage.GetRequestID()
}

// SetRequestID inherits docs from ProxyMessage.SetRequestID()
func (reply *ProxyReply) SetRequestID(value int64) {
	reply.ProxyMessage.SetRequestID(value)
}

// -------------------------------------------------------------------------
// IProxyReply interface methods for implementing the IProxyReply interface

// GetError gets the CadenceError encoded as a JSON string in a ProxyReply's
// Properties map
//
// returns cadenceerrors.CadenceError -> a CadenceError struct encoded with the
// JSON property values at a ProxyReply's Error property
func (reply *ProxyReply) GetError() *cadenceerrors.CadenceError {
	cadenceError := reply.GetJSONProperty("Error", cadenceerrors.NewCadenceErrorEmpty())
	if v, ok := cadenceError.(*cadenceerrors.CadenceError); ok {
		return v
	}

	return nil
}

// SetError sets a CadenceError as a JSON string in a ProxyReply's
// properties map at the Error Property
//
// param cadenceerrors.CadenceError -> the CadenceError to marshal into a
// JSON string and set at a ProxyReply's Error property
func (reply *ProxyReply) SetError(value *cadenceerrors.CadenceError) {
	reply.SetJSONProperty("Error", value)
}
