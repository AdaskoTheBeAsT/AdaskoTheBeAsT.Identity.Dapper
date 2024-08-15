Feature: WithoutNormalizedAspNetIdentityGuidUserStore

A short summary of the feature

@xunit:collection(DatabaseWithGuidIdCollection)
Scenario: Create a new user without normalized and Guid id
	Given I have configured Identity Connection Provider without normalized and Guid id
    When I add the user without normalized and Guid id to the user store
    Then the user without normalized and Guid id should be in the user store
