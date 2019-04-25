package connect

import (
	"fmt"

	"github.com/loopieio/cadence-proxy/cmd/cadenceproxy/messages"
	"github.com/loopieio/cadence-proxy/cmd/cadenceproxy/messages/base"
)

type (

	// ConnectRequest is ProxyRequest of MessageType
	// ConnectRequest.  It holds a reference to a
	// ProxyRequest in memory
	ConnectRequest struct {
		*base.ProxyRequest
	}
)

// InitConnect is a method that adds a key/value entry into the
// IntToMessageStruct at keys ConnectRequest and ConnectReply.
// The values are new instances of a ConnectRequest and ConnectReply
func InitConnect() {
	key := int(messages.ConnectRequest)
	base.IntToMessageStruct[key] = NewConnectRequest()

	key = int(messages.ConnectReply)
	base.IntToMessageStruct[key] = NewConnectReply()
}

// NewConnectRequest is the default constructor for a ConnectRequest
//
// returns *ConnectRequest -> a reference to a newly initialized
// ConnectRequest in memory
func NewConnectRequest() *ConnectRequest {
	request := new(ConnectRequest)
	request.ProxyRequest = base.NewProxyRequest()
	request.Type = messages.ConnectRequest
	return request
}

// GetEndpoints gets a ConnectRequest's endpoints value from
// its nested properties map
//
// returns *string -> a pointer to a string in memory holding the value
// of a ConnectRequest's endpoints
func (request *ConnectRequest) GetEndpoints() *string {
	return request.GetStringProperty(base.EndpointsKey)
}

// SetEndpoints sets a ConnectionRequest's endpoints in
// its nested properties map
//
// param value *string -> a pointer to a string in memory
// that holds the value to be set in the properties map
func (request *ConnectRequest) SetEndpoints(value *string) {
	request.SetStringProperty(base.EndpointsKey, value)
}

// GetDomain gets a ConnectRequest's domain value from
// its nested properties map
//
// returns *string -> a pointer to a string in memory holding the value
// of a ConnectRequest's domain
func (request *ConnectRequest) GetDomain() *string {
	return request.GetStringProperty(base.DomainKey)
}

// SetDomain sets a ConnectionRequest's domain in
// its nested properties map
//
// param value *string -> a pointer to a string in memory
// that holds the value to be set in the properties map
func (request *ConnectRequest) SetDomain(value *string) {
	request.SetStringProperty(base.DomainKey, value)
}

// GetIdentity gets a ConnectRequest's identity value from
// its nested properties map
//
// returns *string -> a pointer to a string in memory holding the value
// of a ConnectRequest's identity
func (request *ConnectRequest) GetIdentity() *string {
	return request.GetStringProperty(base.IdentityKey)
}

// SetIdentity sets a ConnectionRequest's identity in
// its nested properties map
//
// param value *string -> a pointer to a string in memory
// that holds the value to be set in the properties map
func (request *ConnectRequest) SetIdentity(value *string) {
	request.SetStringProperty(base.IdentityKey, value)
}

// -------------------------------------------------------------------------
// IProxyMessage interface methods for implementing the IProxyMessage interface

// Clone inherits docs from ProxyMessage.Clone()
func (request *ConnectRequest) Clone() base.IProxyMessage {
	connectRequest := NewConnectRequest()

	var messageClone base.IProxyMessage = connectRequest
	request.CopyTo(messageClone)

	return messageClone
}

// CopyTo inherits docs from ProxyMessage.CopyTo()
func (request *ConnectRequest) CopyTo(target base.IProxyMessage) {
	request.ProxyRequest.CopyTo(target)
	v, ok := target.(*ConnectRequest)
	if ok {
		v.SetEndpoints(request.GetEndpoints())
		v.SetDomain(request.GetDomain())
		v.SetIdentity(request.GetIdentity())
		v.SetProxyRequest(request.ProxyRequest)
	}
}

// SetProxyMessage inherits docs from ProxyMessage.SetProxyMessage()
func (request *ConnectRequest) SetProxyMessage(value *base.ProxyMessage) {
	*request.ProxyMessage = *value
}

// GetProxyMessage inherits docs from ProxyMessage.GetProxyMessage()
func (request *ConnectRequest) GetProxyMessage() *base.ProxyMessage {
	return request.ProxyMessage
}

// String inherits docs from ProxyMessage.String()
func (request *ConnectRequest) String() string {
	str := ""
	str = fmt.Sprintf("%s\n{\n", str)
	str = fmt.Sprintf("%s%s", str, request.ProxyRequest.String())
	str = fmt.Sprintf("%s}\n", str)
	return str
}

// -------------------------------------------------------------------------
// IProxyRequest interface methods for implementing the IProxyRequest interface

// GetProxyRequest inherits docs from ProxyRequest.GetProxyRequest()
func (request *ConnectRequest) GetProxyRequest() *base.ProxyRequest {
	return request.ProxyRequest
}

// SetProxyRequest inherits docs from ProxyRequest.SetProxyRequest()
func (request *ConnectRequest) SetProxyRequest(value *base.ProxyRequest) {
	*request.ProxyRequest = *value
}