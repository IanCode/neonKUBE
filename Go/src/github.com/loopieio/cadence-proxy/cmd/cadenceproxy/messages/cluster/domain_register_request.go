package cluster

import (
	"github.com/loopieio/cadence-proxy/cmd/cadenceproxy/messages"
	"github.com/loopieio/cadence-proxy/cmd/cadenceproxy/messages/base"
)

type (

	// DomainRegisterRequest is ProxyRequest of MessageType
	// DomainRegisterRequest.
	//
	// A DomainRegisterRequest contains a RequestId and a reference to a
	// ProxyReply struct in memory and ReplyType, which is
	// the corresponding MessageType for replying to this ProxyRequest
	DomainRegisterRequest struct {
		*base.ProxyRequest
	}
)

// NewDomainRegisterRequest is the default constructor for a DomainRegisterRequest
//
// returns *DomainRegisterRequest -> a reference to a newly initialized
// DomainRegisterRequest in memory
func NewDomainRegisterRequest() *DomainRegisterRequest {
	request := new(DomainRegisterRequest)
	request.ProxyRequest = base.NewProxyRequest()
	request.Type = messages.DomainRegisterRequest
	request.SetReplyType(messages.DomainRegisterReply)

	return request
}

// GetName gets a DomainRegisterRequest's Name value
// from its properties map
//
// returns *string -> pointer to a string in memory holding the value
// of a DomainRegisterRequest's Name
func (request *DomainRegisterRequest) GetName() *string {
	return request.GetStringProperty("Name")
}

// SetName sets a DomainRegisterRequest's Name value
// in its properties map
//
// param value *string -> a pointer to a string in memory that holds the value
// to be set in the properties map
func (request *DomainRegisterRequest) SetName(value *string) {
	request.SetStringProperty("Name", value)
}

// GetDescription gets a DomainRegisterRequest's Description value
// from its properties map
//
// returns *string -> pointer to a string in memory holding the value
// of a DomainRegisterRequest's Description
func (request *DomainRegisterRequest) GetDescription() *string {
	return request.GetStringProperty("Description")
}

// SetDescription sets a DomainRegisterRequest's Description value
// in its properties map
//
// param value *string -> a pointer to a string in memory that holds the value
// to be set in the properties map
func (request *DomainRegisterRequest) SetDescription(value *string) {
	request.SetStringProperty("Description", value)
}

// GetOwnerEmail gets a DomainRegisterRequest's OwnerEmail value
// from its properties map
//
// returns *string -> pointer to a string in memory holding the value
// of a DomainRegisterRequest's OwnerEmail
func (request *DomainRegisterRequest) GetOwnerEmail() *string {
	return request.GetStringProperty("OwnerEmail")
}

// SetOwnerEmail sets a DomainRegisterRequest's OwnerEmail value
// in its properties map
//
// param value *string -> a pointer to a string in memory that holds the value
// to be set in the properties map
func (request *DomainRegisterRequest) SetOwnerEmail(value *string) {
	request.SetStringProperty("OwnerEmail", value)
}

// GetEmitMetrics gets a DomainRegisterRequest's EmitMetrics value
// from its properties map
//
// returns bool -> bool indicating whether or not to enable metrics
func (request *DomainRegisterRequest) GetEmitMetrics() bool {
	return request.GetBoolProperty("EmitMetrics")
}

// SetEmitMetrics sets a DomainRegisterRequest's EmitMetrics value
// in its properties map
//
// param value bool -> bool value to be set in the properties map
func (request *DomainRegisterRequest) SetEmitMetrics(value bool) {
	request.SetBoolProperty("EmitMetrics", value)
}

// GetRetentionDays gets a DomainRegisterRequest's RetentionDays value
// from its properties map
//
// returns int32 -> int32 indicating the complete workflow history retention
// period in days
func (request *DomainRegisterRequest) GetRetentionDays() int32 {
	return request.GetIntProperty("RetentionDays")
}

// SetRetentionDays sets a DomainRegisterRequest's EmitMetrics value
// in its properties map
//
// param value int32 -> int32 value to be set in the properties map
func (request *DomainRegisterRequest) SetRetentionDays(value int32) {
	request.SetIntProperty("RetentionDays", value)
}

// -------------------------------------------------------------------------
// IProxyMessage interface methods for implementing the IProxyMessage interface

// Clone inherits docs from ProxyMessage.Clone()
func (request *DomainRegisterRequest) Clone() base.IProxyMessage {
	domainRegisterRequest := NewDomainRegisterRequest()
	var messageClone base.IProxyMessage = domainRegisterRequest
	request.CopyTo(messageClone)

	return messageClone
}

// CopyTo inherits docs from ProxyMessage.CopyTo()
func (request *DomainRegisterRequest) CopyTo(target base.IProxyMessage) {
	request.ProxyRequest.CopyTo(target)
	if v, ok := target.(*DomainRegisterRequest); ok {
		v.SetName(request.GetName())
		v.SetDescription(request.GetDescription())
		v.SetOwnerEmail(request.GetOwnerEmail())
		v.SetEmitMetrics(request.GetEmitMetrics())
		v.SetRetentionDays(request.GetRetentionDays())
	}
}

// SetProxyMessage inherits docs from ProxyMessage.SetProxyMessage()
func (request *DomainRegisterRequest) SetProxyMessage(value *base.ProxyMessage) {
	request.ProxyMessage.SetProxyMessage(value)
}

// GetProxyMessage inherits docs from ProxyMessage.GetProxyMessage()
func (request *DomainRegisterRequest) GetProxyMessage() *base.ProxyMessage {
	return request.ProxyMessage.GetProxyMessage()
}

// GetRequestID inherits docs from ProxyMessage.GetRequestID()
func (request *DomainRegisterRequest) GetRequestID() int64 {
	return request.ProxyMessage.GetRequestID()
}

// SetRequestID inherits docs from ProxyMessage.SetRequestID()
func (request *DomainRegisterRequest) SetRequestID(value int64) {
	request.ProxyMessage.SetRequestID(value)
}

// -------------------------------------------------------------------------
// IProxyRequest interface methods for implementing the IProxyRequest interface

// GetReplyType inherits docs from ProxyRequest.GetReplyType()
func (request *DomainRegisterRequest) GetReplyType() messages.MessageType {
	return request.ProxyRequest.GetReplyType()
}

// SetReplyType inherits docs from ProxyRequest.SetReplyType()
func (request *DomainRegisterRequest) SetReplyType(value messages.MessageType) {
	request.ProxyRequest.SetReplyType(value)
}
