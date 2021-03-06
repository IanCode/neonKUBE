package cluster

import (
	"github.com/loopieio/cadence-proxy/cmd/cadenceproxy/messages"
	"github.com/loopieio/cadence-proxy/cmd/cadenceproxy/messages/base"
)

type (

	// InitializeRequest is ProxyRequest of MessageType
	// InitializeRequest.
	//
	// A InitializeRequest contains a RequestId and a reference to a
	// ProxyReply struct in memory and ReplyType, which is
	// the corresponding MessageType for replying to this ProxyRequest
	InitializeRequest struct {
		*base.ProxyRequest
	}
)

// NewInitializeRequest is the default constructor for a InitializeRequest
//
// returns *InitializeRequest -> pointer to a newly initialized
// InitializeRequest in memory
func NewInitializeRequest() *InitializeRequest {
	request := new(InitializeRequest)
	request.ProxyRequest = base.NewProxyRequest()
	request.Type = messages.InitializeRequest
	request.SetReplyType(messages.InitializeReply)

	return request
}

// GetLibraryAddress gets the LibraryAddress property from an InitializeRequest
// in its properties map
//
// returns *string -> a pointer to a string in memory that holds the value
// of an InitializeRequest's LibraryAddress
func (request *InitializeRequest) GetLibraryAddress() *string {
	return request.GetStringProperty("LibraryAddress")
}

// SetLibraryAddress sets the LibraryAddress property in an INitializeRequest's
// properties map
//
// param value *string -> a pointer to a string that holds the LibraryAddress value
// to set in the request's properties map
func (request *InitializeRequest) SetLibraryAddress(value *string) {
	request.SetStringProperty("LibraryAddress", value)
}

// GetLibraryPort gets the LibraryPort property from an InitializeRequest
// in its properties map
//
// returns *string -> a pointer to a string in memory that holds the value
// of an InitializeRequest's LibraryPort
func (request *InitializeRequest) GetLibraryPort() int32 {
	return request.GetIntProperty("LibraryPort")
}

// SetLibraryPort sets the LibraryPort property in an INitializeRequest's
// properties map
//
// param value *string -> a pointer to a string that holds the LibraryPort value
// to set in the request's properties map
func (request *InitializeRequest) SetLibraryPort(value int32) {
	request.SetIntProperty("LibraryPort", value)
}

// -------------------------------------------------------------------------
// IProxyMessage interface methods for implementing the IProxyMessage interface

// Clone inherits docs from ProxyMessage.Clone()
func (request *InitializeRequest) Clone() base.IProxyMessage {
	initializeRequest := NewInitializeRequest()
	var messageClone base.IProxyMessage = initializeRequest
	request.CopyTo(messageClone)

	return messageClone
}

// CopyTo inherits docs from ProxyMessage.CopyTo()
func (request *InitializeRequest) CopyTo(target base.IProxyMessage) {
	request.ProxyRequest.CopyTo(target)
	if v, ok := target.(*InitializeRequest); ok {
		v.SetLibraryAddress(request.GetLibraryAddress())
		v.SetLibraryPort(request.GetLibraryPort())
	}
}

// SetProxyMessage inherits docs from ProxyMessage.SetProxyMessage()
func (request *InitializeRequest) SetProxyMessage(value *base.ProxyMessage) {
	request.ProxyMessage.SetProxyMessage(value)
}

// GetProxyMessage inherits docs from ProxyMessage.GetProxyMessage()
func (request *InitializeRequest) GetProxyMessage() *base.ProxyMessage {
	return request.ProxyMessage.GetProxyMessage()
}

// GetRequestID inherits docs from ProxyMessage.GetRequestID()
func (request *InitializeRequest) GetRequestID() int64 {
	return request.ProxyMessage.GetRequestID()
}

// SetRequestID inherits docs from ProxyMessage.SetRequestID()
func (request *InitializeRequest) SetRequestID(value int64) {
	request.ProxyMessage.SetRequestID(value)
}

// -------------------------------------------------------------------------
// IProxyRequest interface methods for implementing the IProxyRequest interface

// GetReplyType inherits docs from ProxyRequest.GetReplyType()
func (request *InitializeRequest) GetReplyType() messages.MessageType {
	return request.ProxyRequest.GetReplyType()
}

// SetReplyType inherits docs from ProxyRequest.SetReplyType()
func (request *InitializeRequest) SetReplyType(value messages.MessageType) {
	request.ProxyRequest.SetReplyType(value)
}
