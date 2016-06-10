# encoding: utf-8

$: << 'RspecTests/Generated/model_flattening'

require 'securerandom'
require 'model_flattening'

include ModelFlatteningModule
include ModelFlatteningModule::Models

describe 'ModelFlattening' do
  before(:all) do
    @base_url = ENV['StubServerURI']

    dummyToken = 'dummy12321343423'
    @credentials = MsRest::TokenCredentials.new(dummyToken)

    @client = AutoRestResourceFlatteningTestService.new(@credentials, @base_url)

    product1 = FlattenedProduct.new
    product1.location = "West US"
    product1.tags = {
        "tag1" => "value1",
        "tag2" => "value3"
    }

    # product1.pname = "Product1"

    product2 = FlattenedProduct.new
    product2.location = "Building 44"
    # product2.pname = "Product2"

    @dict_resource = {
        "Resource1" => product1,
        "Resource2" => product2
    }

    @array_resource = [product1, product2]
  end

  # Array tests
  it 'should get array' do
    result = @client.get_array_async().value!
    
    # Resource 1
    expect(result.body.count).to eq(3)
    expect(result.body[0].id).to eq("1")
    expect(result.body[0].name).to eq("Resource1")
    expect(result.body[0].location).to eq("Building 44")
    expect(result.body[0].type).to eq("Microsoft.Web/sites")
    expect(result.body[0].pname).to eq("Product1")
    expect(result.body[0].provisioning_state).to eq("Succeeded")
    expect(result.body[0].provisioning_state_values).to eq("OK")
    expect(result.body[0].tags["tag1"]).to eq("value1")
    expect(result.body[0].tags["tag2"]).to eq("value3")

    # Resource 2
    expect(result.body[1].id).to eq("2")
    expect(result.body[1].location).to eq("Building 44")
    expect(result.body[1].name).to eq("Resource2")

    # Resource 3
    expect(result.body[2].id).to eq("3")
    expect(result.body[2].name).to eq("Resource3")
  end

  # it 'should put array' do
  #   result = @client.resource_flattening.put_array_async(@array_resource).value!
  #   expect(result).to be_an_instance_of(Net::HTTPOK)
  # end

  # Dictionary tests
  it 'should get dictionary' do
    result = @client.get_dictionary_async().value!
    # Resource 1
    expect(result.body.count).to eq(3)
    expect(result.body["Product1"].id).to eq("1")
    expect(result.body["Product1"].location).to eq("Building 44")
    expect(result.body["Product1"].name).to eq("Resource1")
    expect(result.body["Product1"].type).to eq("Microsoft.Web/sites")

    expect(result.body["Product1"].provisioning_state_values).to eq("OK")
    expect(result.body["Product1"].pname).to eq("Product1")
    expect(result.body["Product1"].provisioning_state).to eq("Succeeded")

    expect(result.body["Product1"].tags["tag1"]).to eq("value1")
    expect(result.body["Product1"].tags["tag2"]).to eq("value3")

    # Resource 2
    expect(result.body["Product2"].id).to eq("2")
    expect(result.body["Product2"].location).to eq("Building 44")
    expect(result.body["Product2"].name).to eq("Resource2")

    # Resource 3
    expect(result.body["Product3"].id).to eq("3")
    expect(result.body["Product3"].name).to eq("Resource3")
  end

  # it 'should put dictionary' do
  #   result = @client.resource_flattening.put_dictionary_async(@dict_resource).value!.response
  #   expect(result).to be_an_instance_of(Net::HTTPOK)
  # end

  # Complex tests
  it 'should get resource collection' do
    result = @client.get_resource_collection_async().value!

    # Resource 1
    expect(result.body.dictionaryofresources.count).to eq(3)
    expect(result.body.dictionaryofresources["Product1"].id).to eq("1")
    expect(result.body.dictionaryofresources["Product1"].location).to eq("Building 44")
    expect(result.body.dictionaryofresources["Product1"].name).to eq("Resource1")
    expect(result.body.dictionaryofresources["Product1"].type).to eq("Microsoft.Web/sites")

    expect(result.body.dictionaryofresources["Product1"].tags["tag1"]).to eq("value1")
    expect(result.body.dictionaryofresources["Product1"].tags["tag2"]).to eq("value3")

    expect(result.body.dictionaryofresources["Product1"].provisioning_state_values).to eq("OK")
    expect(result.body.dictionaryofresources["Product1"].pname).to eq("Product1")
    expect(result.body.dictionaryofresources["Product1"].provisioning_state).to eq("Succeeded")

    # Resource 2
    expect(result.body.dictionaryofresources["Product2"].id).to eq("2")
    expect(result.body.dictionaryofresources["Product2"].location).to eq("Building 44")
    expect(result.body.dictionaryofresources["Product2"].name).to eq("Resource2")

    # Resource 3
    expect(result.body.dictionaryofresources["Product3"].id).to eq("3")
    expect(result.body.dictionaryofresources["Product3"].name).to eq("Resource3")
    expect(result.body.arrayofresources.count).to eq(3)

    # Resource 1
    expect(result.body.arrayofresources[0].id).to eq("4")
    expect(result.body.arrayofresources[0].name).to eq("Resource4")
    expect(result.body.arrayofresources[0].location).to eq("Building 44")
    expect(result.body.arrayofresources[0].type).to eq("Microsoft.Web/sites")

    expect(result.body.arrayofresources[0].provisioning_state_values).to eq("OK")
    expect(result.body.arrayofresources[0].pname).to eq("Product4")
    expect(result.body.arrayofresources[0].provisioning_state).to eq("Succeeded")

    expect(result.body.arrayofresources[0].tags["tag1"]).to eq("value1")
    expect(result.body.arrayofresources[0].tags["tag2"]).to eq("value3")

    # Resource 2
    expect(result.body.arrayofresources[1].id).to eq("5")
    expect(result.body.arrayofresources[1].location).to eq("Building 44")
    expect(result.body.arrayofresources[1].name).to eq("Resource5")

    # Resource 3
    expect(result.body.arrayofresources[2].id).to eq("6")
    expect(result.body.arrayofresources[2].name).to eq("Resource6")
  end

  # it 'should put complex object' do
  #   complex_resource = ResourceCollection.new
  #   complex_resource.dictionaryOfResources = @dict_resource
  #   complex_resource.arrayOfResources = @array_resource
  #   result = @client.resource_flattening.put_resource_collection_async(complex_resource).value!.response
  #   expect(result).to be_an_instance_of(Net::HTTPOK)
  # end

end