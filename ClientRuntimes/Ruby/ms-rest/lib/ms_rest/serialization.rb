# encoding: utf-8
# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License. See License.txt in the project root for license information.

module MsRest
  # Base module for Ruby serialization and deserialization.
  #
  # Provides methods to serialize Ruby object into Ruby Hash and
  # to deserialize Ruby Hash into Ruby object.
  module Serialization
    #
    # Deserialize the response from the server using the mapper.
    #
    # @param mapper [Hash] Ruby Hash object to represent expected structure of the response_body.
    # @param response_body [Hash] Ruby Hash object to deserialize.
    # @param object_name [String] Name of the deserialized object.
    #
    def deserialize(mapper, response_body, object_name)
      serialization = Serialization.new(self)
      serialization.deserialize(mapper, response_body, object_name)
    end

    #
    # Serialize the Ruby object into Ruby Hash to send it to the server using the mapper.
    #
    # @param mapper [Hash] Ruby Hash object to represent expected structure of the object.
    # @param object [Object] Ruby object to serialize.
    # @param object_name [String] Name of the serialized object.
    #
    def serialize(mapper, object, object_name)
      serialization = Serialization.new(self)
      serialization.serialize(mapper, object, object_name)
    end

    #
    # Class to handle serialization & deserialization.
    #
    class Serialization
      def initialize(context)
        @context = context
      end

      #
      # Deserialize the response from the server using the mapper.
      #
      # @param mapper [Hash] Ruby Hash object to represent expected structure of the response_body.
      # @param response_body [Hash] Ruby Hash object to deserialize.
      # @param object_name [String] Name of the deserialized object.
      #
      def deserialize(mapper, response_body, object_name)
        return response_body if response_body.nil?
        object_name = mapper[:serializedName] unless object_name.nil?

        mapper_type = mapper[:type][:name]

        if !mapper_type.match(/^(Number|Double|ByteArray|Boolean|Date|DateTime|DateTimeRfc1123|UnixTime|Enum|String)$/i).nil?
          payload = deserialize_primary_type(mapper, response_body)
        elsif !mapper_type.match(/^Dictionary$/i).nil?
          payload = deserialize_dictionary_type(mapper, response_body, object_name)
        elsif !mapper_type.match(/^Composite$/i).nil?
          payload = deserialize_composite_type(mapper, response_body, object_name)
        elsif !mapper_type.match(/^Sequence$/i).nil?
          payload = deserialize_sequence_type(mapper, response_body, object_name)
        else
          payload = ""
        end
        payload
      end

      #
      # Deserialize the response of known primary type from the server using the mapper.
      #
      # @param mapper [Hash] Ruby Hash object to represent expected structure of the response_body.
      # @param response_body [Hash] Ruby Hash object to deserialize.
      #
      def deserialize_primary_type(mapper, response_body)
        result = ""
        case mapper[:type][:name]
          when 'Number'
            result = Integer(response_body)
          when 'Double'
            result = Float(response_body) unless response_body.to_s.empty?
          when 'ByteArray'
            result = Base64.strict_decode64(response_body).unpack('C*') unless response_body.to_s.empty?
          when 'String', 'Enum', 'Boolean'
            result = response_body
          when 'Date'
            unless response_body.to_s.empty?
              result = Timeliness.parse(response_body, :strict => true)
              fail DeserializationError.new('Error occured in deserializing the response_body', nil, nil, response_body) if result.nil?
              result = ::Date.parse(result.to_s)
            end
          when 'DateTime', 'DateTimeRfc1123'
            result = DateTime.parse(response_body) unless response_body.to_s.empty?
          when 'UnixTime'
            result = DateTime.strptime(response_body.to_s, '%s') unless response_body.to_s.empty?
          else
            result
        end
        result
      end

      #
      # Deserialize the response of dictionary type from the server using the mapper.
      #
      # @param mapper [Hash] Ruby Hash object to represent expected structure of the response_body.
      # @param response_body [Hash] Ruby Hash object to deserialize.
      # @param object_name [String] Name of the deserialized object.
      #
      def deserialize_dictionary_type(mapper, response_body, object_name)
        if mapper[:type][:value].nil? || !mapper[:type][:value].is_a?(Hash)
          fail DeserializationError.new("'value' metadata for a dictionary type must be defined in the mapper and it must be of type Hash in #{object_name}", nil, nil, response_body)
        end

        result = Hash.new
        response_body.each do |key, val|
          result[key] = deserialize(mapper[:type][:value], val, object_name)
        end
        result
      end

      #
      # Deserialize the response of composite type from the server using the mapper.
      #
      # @param mapper [Hash] Ruby Hash object to represent expected structure of the response_body.
      # @param response_body [Hash] Ruby Hash object to deserialize.
      # @param object_name [String] Name of the deserialized object.
      #
      def deserialize_composite_type(mapper, response_body, object_name)
        if mapper[:type][:className].nil?
          fail DeserializationError.new("'className' metadata for a composite type must be defined in the mapper and it must be of type Hash in #{object_name}", nil, nil, response_body)
        end

        if !mapper[:type][:polymorphicDiscriminator].nil?
          # Handle polymorphic types
          parent_class = get_model(mapper[:type][:className])
          discriminator = parent_class.class_eval("@@discriminatorMap")
          model_name = response_body["#{mapper[:type][:polymorphicDiscriminator]}"]
          model_class = get_model(discriminator[model_name].capitalize)
        else
          model_class = get_model(mapper[:type][:className])
        end

        result = model_class.new

        model_mapper = model_class.mapper()
        model_props = model_mapper[:type][:modelProperties]

        unless model_props.nil?
          model_props.each do |key, val|
            result.instance_variable_set("@#{key}", deserialize(val, response_body[val[:serializedName].to_s], object_name))
          end
        end
        result
      end

      #
      # Deserialize the response of sequence type from the server using the mapper.
      #
      # @param mapper [Hash] Ruby Hash object to represent expected structure of the response_body.
      # @param response_body [Hash] Ruby Hash object to deserialize.
      # @param object_name [String] Name of the deserialized object.
      #
      def deserialize_sequence_type(mapper, response_body, object_name)
        if mapper[:type][:element].nil? || !mapper[:type][:element].is_a?(Hash)
          fail DeserializationError.new("'element' metadata for a sequence type must be defined in the mapper and it must be of type Hash in #{object_name}", nil, nil, response_body)
        end

        return response_body if response_body.nil?

        result = []
        response_body.each do |element|
          result.push(deserialize(mapper[:type][:element], element, object_name))
        end

        result
      end

      #
      # Serialize the Ruby object into Ruby Hash to send it to the server using the mapper.
      #
      # @param mapper [Hash] Ruby Hash object to represent expected structure of the object.
      # @param object [Object] Ruby object to serialize.
      # @param object_name [String] Name of the serialized object.
      #
      def serialize(mapper, object, object_name)
        object_name = mapper[:serializedName] unless object_name.nil?

        if mapper[:required] && object.nil? && !mapper[:isConstant]
          fail ValidationError, "#{object_name} is required and cannot be nil"
        end

        if !mapper[:required] && object.nil?
          return object
        end

        # Set defaults
        unless mapper[:defaultValue].nil?
          object = mapper[:defaultValue] if object.nil?
        end
        object = mapper[:defaultValue] if mapper[:isConstant]

        payload = Hash.new
        mapper_type = mapper[:type][:name]
        if !mapper_type.match(/^(Number|Double|ByteArray|Boolean|Date|DateTime|DateTimeRfc1123|UnixTime|Enum|String)$/i).nil?
          payload = serialize_primary_type(mapper, object)
        elsif !mapper_type.match(/^Dictionary$/i).nil?
          payload = serialize_dictionary_type(mapper, object, object_name)
        elsif !mapper_type.match(/^Composite$/i).nil?
          payload = serialize_composite_type(mapper, object, object_name)
        elsif !mapper_type.match(/^Sequence$/i).nil?
          payload = serialize_sequence_type(mapper, object, object_name)
        end
        payload
      end

      #
      # Serialize the Ruby object of known primary type into Ruby Hash to send it to the server using the mapper.
      #
      # @param mapper [Hash] Ruby Hash object to represent expected structure of the object.
      # @param object [Object] Ruby object to serialize.
      #
      def serialize_primary_type(mapper, object)
        mapper_type = mapper[:type][:name]
        payload = nil
        case mapper_type
          when 'Number', 'Double', 'String', 'Enum', 'Date', 'Boolean'
            payload = object != nil ? object : nil
          when 'ByteArray'
            payload = Base64.strict_encode64(object.pack('c*'))
          when 'DateTime'
            payload = object.new_offset(0).strftime('%FT%TZ')
          when 'DateTimeRfc1123'
            payload = object.new_offset(0).strftime('%a, %d %b %Y %H:%M:%S GMT')
          when 'UnixTime'
            payload = object.new_offset(0).strftime('%s') unless object.nil?
        end
        payload
      end

      #
      # Serialize the Ruby object of dictionary type into Ruby Hash to send it to the server using the mapper.
      #
      # @param mapper [Hash] Ruby Hash object to represent expected structure of the object.
      # @param object [Object] Ruby object to serialize.
      # @param object_name [String] Name of the serialized object.
      #
      def serialize_dictionary_type(mapper, object, object_name)
        unless object.is_a?(Hash)
          fail DeserializationError.new("#{object_name} must be of type Hash", nil, nil, object)
        end

        unless mapper[:type][:value].nil? || mapper[:type][:value].is_a?(Hash)
          fail DeserializationError.new("'value' metadata for a dictionary type must be defined in the mapper and it must be of type Hash in #{object_name}", nil, nil, object)
        end

        payload = Hash.new
        object.each do |key, value|
          if !value.nil? && value.respond_to?(:validate)
            value.validate
          end

          payload[key] = serialize(mapper[:type][:value], value, object_name)
        end
        payload
      end

      #
      # Serialize the Ruby object of composite type into Ruby Hash to send it to the server using the mapper.
      #
      # @param mapper [Hash] Ruby Hash object to represent expected structure of the object.
      # @param object [Object] Ruby object to serialize.
      # @param object_name [String] Name of the serialized object.
      #
      def serialize_composite_type(mapper, object, object_name)
        if !mapper[:type][:polymorphicDiscriminator].nil?
          # Handle polymorphic types
          model_name = object.class.to_s.split('::')[-1]
          model_class = get_model(model_name)
        else
          model_class = get_model(mapper[:type][:className])
        end

        payload = Hash.new
        model_mapper = model_class.mapper()
        model_props = model_mapper[:type][:modelProperties]

        unless model_props.nil?
          model_props.each do |key, value|
            instance_variable = object.instance_variable_get("@#{key}")
            if !instance_variable.nil? && instance_variable.respond_to?(:validate)
              instance_variable.validate
            end

            sub_payload = serialize(value, instance_variable, object_name)
            payload[value[:serializedName].to_s] = sub_payload unless instance_variable.nil?
          end
        end
        payload
      end

      #
      # Serialize the Ruby object of sequence type into Ruby Hash to send it to the server using the mapper.
      #
      # @param mapper [Hash] Ruby Hash object to represent expected structure of the object.
      # @param object [Object] Ruby object to serialize.
      # @param object_name [String] Name of the serialized object.
      #
      def serialize_sequence_type(mapper, object, object_name)
        unless object.is_a?(Array)
          fail DeserializationError.new("#{object_name} must be of type of Array", nil, nil, object)
        end

        unless mapper[:type][:element].nil? || mapper[:type][:element].is_a?(Hash)
          fail DeserializationError.new("'element' metadata for a sequence type must be defined in the mapper and it must be of type Hash in #{object_name}", nil, nil, object)
        end

        payload = Array.new
        object.each do |element|
          if !element.nil? && element.respond_to?(:validate)
            element.validate
          end
          payload.push(serialize(mapper[:type][:element], element, object_name))
        end
        payload
      end

      #
      # Retrieves model of the model_name
      #
      # @param model_name [String] Name of the model to retrieve.
      #
      def get_model(model_name)
        Object.const_get(@context.class.to_s.split('::')[0...-1].join('::') + "::Models::#{model_name}")
      end
    end
  end
end
