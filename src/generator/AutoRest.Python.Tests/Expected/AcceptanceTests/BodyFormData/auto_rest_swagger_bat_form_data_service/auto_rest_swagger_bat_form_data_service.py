# coding=utf-8
# --------------------------------------------------------------------------
# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License. See License.txt in the project root for
# license information.
#
# Code generated by Microsoft (R) AutoRest Code Generator.
# Changes may cause incorrect behavior and will be lost if the code is
# regenerated.
# --------------------------------------------------------------------------

from msrest.service_client import ServiceClient
from msrest import Configuration, Serializer, Deserializer
from .version import VERSION
from .operations.formdata_operations import FormdataOperations
from . import models


class AutoRestSwaggerBATFormDataServiceConfiguration(Configuration):
    """Configuration for AutoRestSwaggerBATFormDataService
    Note that all parameters used to create this instance are saved as instance
    attributes.

    :param str base_url: Service URL
    """

    def __init__(
            self, base_url=None):

        if not base_url:
            base_url = 'http://localhost'

        super(AutoRestSwaggerBATFormDataServiceConfiguration, self).__init__(base_url)

        self.add_user_agent('autorestswaggerbatformdataservice/{}'.format(VERSION))


class AutoRestSwaggerBATFormDataService(object):
    """Test Infrastructure for AutoRest Swagger BAT

    :ivar config: Configuration for client.
    :vartype config: AutoRestSwaggerBATFormDataServiceConfiguration

    :ivar formdata: Formdata operations
    :vartype formdata: fixtures.acceptancetestsbodyformdata.operations.FormdataOperations

    :param str base_url: Service URL
    """

    def __init__(
            self, base_url=None):

        self.config = AutoRestSwaggerBATFormDataServiceConfiguration(base_url)
        self._client = ServiceClient(None, self.config)

        client_models = {k: v for k, v in models.__dict__.items() if isinstance(v, type)}
        self.api_version = '1.0.0'
        self._serialize = Serializer(client_models)
        self._deserialize = Deserializer(client_models)

        self.formdata = FormdataOperations(
            self._client, self.config, self._serialize, self._deserialize)
