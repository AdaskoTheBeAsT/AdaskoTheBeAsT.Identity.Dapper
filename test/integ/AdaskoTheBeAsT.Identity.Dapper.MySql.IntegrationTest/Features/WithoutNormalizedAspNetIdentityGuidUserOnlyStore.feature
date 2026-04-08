Feature: WithoutNormalizedAspNetIdentityGuidUserOnlyStore

A short summary of the feature

@xunit:collection[DatabaseWithGuidIdCollection]
Scenario Outline: Verify lightweight UserOnlyStore method <method> without normalized and Guid id
	Given I have configured MySQL UserOnlyStore without normalized and Guid id
	When I verify "<method>" on MySQL UserOnlyStore without normalized and Guid id
	Then "<method>" on MySQL UserOnlyStore should work without normalized and Guid id

Examples:
	| method                       |
	| Dispose                      |
	| GetUserIdAsync               |
	| GetUserNameAsync             |
	| SetUserNameAsync             |
	| GetNormalizedUserNameAsync   |
	| SetNormalizedUserNameAsync   |
	| ConvertIdFromString          |
	| ConvertIdToString            |
	| SetPasswordHashAsync         |
	| GetPasswordHashAsync         |
	| HasPasswordAsync             |
	| GetEmailConfirmedAsync       |
	| SetEmailConfirmedAsync       |
	| SetEmailAsync                |
	| GetEmailAsync                |
	| GetNormalizedEmailAsync      |
	| SetNormalizedEmailAsync      |
	| GetLockoutEndDateAsync       |
	| SetLockoutEndDateAsync       |
	| ResetAccessFailedCountAsync  |
	| GetAccessFailedCountAsync    |
	| GetLockoutEnabledAsync       |
	| SetLockoutEnabledAsync       |
	| SetPhoneNumberAsync          |
	| GetPhoneNumberAsync          |
	| GetPhoneNumberConfirmedAsync |
	| SetPhoneNumberConfirmedAsync |
	| SetSecurityStampAsync        |
	| GetSecurityStampAsync        |
	| SetTwoFactorEnabledAsync     |
	| GetTwoFactorEnabledAsync     |

@xunit:collection[DatabaseWithGuidIdCollection]
Scenario: Query Users with Users without normalized and Guid id
	Given I have configured MySQL UserOnlyStore without normalized and Guid id
	And I created users for MySQL UserOnlyStore
		| UserName | Email             |
		| John     | john@example.com  |
	And I created roles for MySQL UserOnlyStore
		| Name  |
		| Admin |
	And I added users to roles for MySQL UserOnlyStore
		| UserName | RoleName |
		| John     | Admin    |
	When I execute "Users" on MySQL UserOnlyStore
	Then the last users result for MySQL UserOnlyStore should match
		| UserName | Email            |
		| John     | john@example.com |

@xunit:collection[DatabaseWithGuidIdCollection]
Scenario: Create user with CreateAsync without normalized and Guid id
	Given I have configured MySQL UserOnlyStore without normalized and Guid id
	When I execute "CreateAsync" on MySQL UserOnlyStore with parameters
		| UserName | Email            |
		| John     | john@example.com |
	Then the last identity result for MySQL UserOnlyStore should be successful
	And the last user result for MySQL UserOnlyStore should match
		| UserName | Email            |
		| John     | john@example.com |

@xunit:collection[DatabaseWithGuidIdCollection]
Scenario: Update user with UpdateAsync without normalized and Guid id
	Given I have configured MySQL UserOnlyStore without normalized and Guid id
	And I created users for MySQL UserOnlyStore
		| UserName | Email            |
		| John     | john@example.com |
	When I execute "UpdateAsync" on MySQL UserOnlyStore with parameters
		| UserName | NewUserName | NewEmail              |
		| John     | Johnny      | johnny@example.com    |
	Then the last identity result for MySQL UserOnlyStore should be successful
	And the last user result for MySQL UserOnlyStore should match
		| UserName | Email               |
		| Johnny   | johnny@example.com  |

@xunit:collection[DatabaseWithGuidIdCollection]
Scenario: Delete user with DeleteAsync without normalized and Guid id
	Given I have configured MySQL UserOnlyStore without normalized and Guid id
	And I created users for MySQL UserOnlyStore
		| UserName | Email            |
		| John     | john@example.com |
	When I execute "DeleteAsync" on MySQL UserOnlyStore with parameters
		| UserName |
		| John     |
	Then the last identity result for MySQL UserOnlyStore should be successful
	And the last user result for MySQL UserOnlyStore should be null

@xunit:collection[DatabaseWithGuidIdCollection]
Scenario: Find user with FindByIdAsync without normalized and Guid id
	Given I have configured MySQL UserOnlyStore without normalized and Guid id
	And I created users for MySQL UserOnlyStore
		| UserName | Email            |
		| John     | john@example.com |
	When I execute "FindByIdAsync" on MySQL UserOnlyStore with parameters
		| UserName |
		| John     |
	Then the last user result for MySQL UserOnlyStore should match
		| UserName | Email            |
		| John     | john@example.com |

@xunit:collection[DatabaseWithGuidIdCollection]
Scenario: Find user with FindByNameAsync without normalized and Guid id
	Given I have configured MySQL UserOnlyStore without normalized and Guid id
	And I created users for MySQL UserOnlyStore
		| UserName | Email            |
		| John     | john@example.com |
	When I execute "FindByNameAsync" on MySQL UserOnlyStore with parameters
		| UserName |
		| John     |
	Then the last user result for MySQL UserOnlyStore should match
		| UserName | Email            |
		| John     | john@example.com |

@xunit:collection[DatabaseWithGuidIdCollection]
Scenario: Find user with FindByEmailAsync without normalized and Guid id
	Given I have configured MySQL UserOnlyStore without normalized and Guid id
	And I created users for MySQL UserOnlyStore
		| UserName | Email            |
		| John     | john@example.com |
	When I execute "FindByEmailAsync" on MySQL UserOnlyStore with parameters
		| Email            |
		| john@example.com |
	Then the last user result for MySQL UserOnlyStore should match
		| UserName | Email            |
		| John     | john@example.com |

@xunit:collection[DatabaseWithGuidIdCollection]
Scenario: Get user claims with GetClaimsAsync without normalized and Guid id
	Given I have configured MySQL UserOnlyStore without normalized and Guid id
	And I created users for MySQL UserOnlyStore
		| UserName | Email            |
		| John     | john@example.com |
	And I added user claims for MySQL UserOnlyStore
		| UserName | ClaimType  | ClaimValue |
		| John     | permission | read       |
		| John     | permission | write      |
	When I execute "GetClaimsAsync" on MySQL UserOnlyStore with parameters
		| UserName |
		| John     |
	Then the last claims result for MySQL UserOnlyStore should match
		| ClaimType  | ClaimValue |
		| permission | read       |
		| permission | write      |

@xunit:collection[DatabaseWithGuidIdCollection]
Scenario: Add user claims with AddClaimsAsync without normalized and Guid id
	Given I have configured MySQL UserOnlyStore without normalized and Guid id
	And I created users for MySQL UserOnlyStore
		| UserName | Email            |
		| John     | john@example.com |
	When I execute "AddClaimsAsync" on MySQL UserOnlyStore with parameters
		| UserName | ClaimType  | ClaimValue |
		| John     | permission | read       |
		| John     | permission | write      |
	Then the last claims result for MySQL UserOnlyStore should match
		| ClaimType  | ClaimValue |
		| permission | read       |
		| permission | write      |

@xunit:collection[DatabaseWithGuidIdCollection]
Scenario: Replace user claim with ReplaceClaimAsync without normalized and Guid id
	Given I have configured MySQL UserOnlyStore without normalized and Guid id
	And I created users for MySQL UserOnlyStore
		| UserName | Email            |
		| John     | john@example.com |
	And I added user claims for MySQL UserOnlyStore
		| UserName | ClaimType  | ClaimValue |
		| John     | permission | read       |
	When I execute "ReplaceClaimAsync" on MySQL UserOnlyStore with parameters
		| UserName | ClaimTypeOld | ClaimValueOld | ClaimTypeNew | ClaimValueNew |
		| John     | permission   | read          | permission   | write         |
	Then the last claims result for MySQL UserOnlyStore should match
		| ClaimType  | ClaimValue |
		| permission | write      |

@xunit:collection[DatabaseWithGuidIdCollection]
Scenario: Remove user claims with RemoveClaimsAsync without normalized and Guid id
	Given I have configured MySQL UserOnlyStore without normalized and Guid id
	And I created users for MySQL UserOnlyStore
		| UserName | Email            |
		| John     | john@example.com |
	And I added user claims for MySQL UserOnlyStore
		| UserName | ClaimType  | ClaimValue |
		| John     | permission | read       |
	When I execute "RemoveClaimsAsync" on MySQL UserOnlyStore with parameters
		| UserName | ClaimType  | ClaimValue |
		| John     | permission | read       |
	Then the last claims result for MySQL UserOnlyStore should match
		| ClaimType | ClaimValue |

@xunit:collection[DatabaseWithGuidIdCollection]
Scenario: Add user login with AddLoginAsync without normalized and Guid id
	Given I have configured MySQL UserOnlyStore without normalized and Guid id
	And I created users for MySQL UserOnlyStore
		| UserName | Email            |
		| John     | john@example.com |
	When I execute "AddLoginAsync" on MySQL UserOnlyStore with parameters
		| UserName | LoginProvider | ProviderKey | ProviderDisplayName |
		| John     | GitHub        | john-gh     | GitHub              |
	Then the last logins result for MySQL UserOnlyStore should match
		| LoginProvider | ProviderKey | ProviderDisplayName |
		| GitHub        | john-gh     | GitHub              |

@xunit:collection[DatabaseWithGuidIdCollection]
Scenario: Remove user login with RemoveLoginAsync without normalized and Guid id
	Given I have configured MySQL UserOnlyStore without normalized and Guid id
	And I created users for MySQL UserOnlyStore
		| UserName | Email            |
		| John     | john@example.com |
	And I added user logins for MySQL UserOnlyStore
		| UserName | LoginProvider | ProviderKey | ProviderDisplayName |
		| John     | GitHub        | john-gh     | GitHub              |
	When I execute "RemoveLoginAsync" on MySQL UserOnlyStore with parameters
		| UserName | LoginProvider | ProviderKey |
		| John     | GitHub        | john-gh     |
	Then the last logins result for MySQL UserOnlyStore should match
		| LoginProvider | ProviderKey | ProviderDisplayName |

@xunit:collection[DatabaseWithGuidIdCollection]
Scenario: Get user logins with GetLoginsAsync without normalized and Guid id
	Given I have configured MySQL UserOnlyStore without normalized and Guid id
	And I created users for MySQL UserOnlyStore
		| UserName | Email            |
		| John     | john@example.com |
	And I added user logins for MySQL UserOnlyStore
		| UserName | LoginProvider | ProviderKey | ProviderDisplayName |
		| John     | GitHub        | john-gh     | GitHub              |
	When I execute "GetLoginsAsync" on MySQL UserOnlyStore with parameters
		| UserName |
		| John     |
	Then the last logins result for MySQL UserOnlyStore should match
		| LoginProvider | ProviderKey | ProviderDisplayName |
		| GitHub        | john-gh     | GitHub              |

@xunit:collection[DatabaseWithGuidIdCollection]
Scenario: Find user by login with FindByLoginAsync without normalized and Guid id
	Given I have configured MySQL UserOnlyStore without normalized and Guid id
	And I created users for MySQL UserOnlyStore
		| UserName | Email            |
		| John     | john@example.com |
	And I added user logins for MySQL UserOnlyStore
		| UserName | LoginProvider | ProviderKey | ProviderDisplayName |
		| John     | GitHub        | john-gh     | GitHub              |
	When I execute "FindByLoginAsync" on MySQL UserOnlyStore with parameters
		| LoginProvider | ProviderKey |
		| GitHub        | john-gh     |
	Then the last user result for MySQL UserOnlyStore should match
		| UserName | Email            |
		| John     | john@example.com |

@xunit:collection[DatabaseWithGuidIdCollection]
Scenario: Increment access failed count with IncrementAccessFailedCountAsync without normalized and Guid id
	Given I have configured MySQL UserOnlyStore without normalized and Guid id
	And I created users for MySQL UserOnlyStore
		| UserName | Email            | AccessFailedCount |
		| John     | john@example.com | 0                 |
	When I execute "IncrementAccessFailedCountAsync" on MySQL UserOnlyStore with parameters
		| UserName |
		| John     |
	Then the last integer result for MySQL UserOnlyStore should be 1
	And the last user result for MySQL UserOnlyStore should match
		| UserName | AccessFailedCount |
		| John     | 1                 |

@xunit:collection[DatabaseWithGuidIdCollection]
Scenario: Get users for claim with GetUsersForClaimAsync without normalized and Guid id
	Given I have configured MySQL UserOnlyStore without normalized and Guid id
	And I created users for MySQL UserOnlyStore
		| UserName | Email            |
		| John     | john@example.com |
	And I added user claims for MySQL UserOnlyStore
		| UserName | ClaimType  | ClaimValue |
		| John     | permission | read       |
	When I execute "GetUsersForClaimAsync" on MySQL UserOnlyStore with parameters
		| ClaimType  | ClaimValue |
		| permission | read       |
	Then the last users result for MySQL UserOnlyStore should match
		| UserName | Email            |
		| John     | john@example.com |

@xunit:collection[DatabaseWithGuidIdCollection]
Scenario: Set token with SetTokenAsync without normalized and Guid id
	Given I have configured MySQL UserOnlyStore without normalized and Guid id
	And I created users for MySQL UserOnlyStore
		| UserName | Email            |
		| John     | john@example.com |
	When I execute "SetTokenAsync" on MySQL UserOnlyStore with parameters
		| UserName | LoginProvider | Name          | Value         |
		| John     | api           | refresh-token | refresh-value |
	Then the last string result for MySQL UserOnlyStore should be "refresh-value"

@xunit:collection[DatabaseWithGuidIdCollection]
Scenario: Remove token with RemoveTokenAsync without normalized and Guid id
	Given I have configured MySQL UserOnlyStore without normalized and Guid id
	And I created users for MySQL UserOnlyStore
		| UserName | Email            |
		| John     | john@example.com |
	And I added user tokens for MySQL UserOnlyStore
		| UserName | LoginProvider | Name          | Value         |
		| John     | api           | refresh-token | refresh-value |
	When I execute "RemoveTokenAsync" on MySQL UserOnlyStore with parameters
		| UserName | LoginProvider | Name          |
		| John     | api           | refresh-token |
	Then the last string result for MySQL UserOnlyStore should be null

@xunit:collection[DatabaseWithGuidIdCollection]
Scenario: Get token with GetTokenAsync without normalized and Guid id
	Given I have configured MySQL UserOnlyStore without normalized and Guid id
	And I created users for MySQL UserOnlyStore
		| UserName | Email            |
		| John     | john@example.com |
	And I added user tokens for MySQL UserOnlyStore
		| UserName | LoginProvider | Name          | Value         |
		| John     | api           | refresh-token | refresh-value |
	When I execute "GetTokenAsync" on MySQL UserOnlyStore with parameters
		| UserName | LoginProvider | Name          |
		| John     | api           | refresh-token |
	Then the last string result for MySQL UserOnlyStore should be "refresh-value"

@xunit:collection[DatabaseWithGuidIdCollection]
Scenario: Set authenticator key with SetAuthenticatorKeyAsync without normalized and Guid id
	Given I have configured MySQL UserOnlyStore without normalized and Guid id
	And I created users for MySQL UserOnlyStore
		| UserName | Email            |
		| John     | john@example.com |
	When I execute "SetAuthenticatorKeyAsync" on MySQL UserOnlyStore with parameters
		| UserName | Key      |
		| John     | auth-key |
	Then the last string result for MySQL UserOnlyStore should be "auth-key"

@xunit:collection[DatabaseWithGuidIdCollection]
Scenario: Get authenticator key with GetAuthenticatorKeyAsync without normalized and Guid id
	Given I have configured MySQL UserOnlyStore without normalized and Guid id
	And I created users for MySQL UserOnlyStore
		| UserName | Email            |
		| John     | john@example.com |
	And I set authenticator keys for MySQL UserOnlyStore
		| UserName | Key      |
		| John     | auth-key |
	When I execute "GetAuthenticatorKeyAsync" on MySQL UserOnlyStore with parameters
		| UserName |
		| John     |
	Then the last string result for MySQL UserOnlyStore should be "auth-key"

@xunit:collection[DatabaseWithGuidIdCollection]
Scenario: Count recovery codes with CountCodesAsync without normalized and Guid id
	Given I have configured MySQL UserOnlyStore without normalized and Guid id
	And I created users for MySQL UserOnlyStore
		| UserName | Email            |
		| John     | john@example.com |
	And I replaced recovery codes for MySQL UserOnlyStore
		| UserName | RecoveryCodes          |
		| John     | code-1;code-2;code-3  |
	When I execute "CountCodesAsync" on MySQL UserOnlyStore with parameters
		| UserName |
		| John     |
	Then the last integer result for MySQL UserOnlyStore should be 3

@xunit:collection[DatabaseWithGuidIdCollection]
Scenario: Replace recovery codes with ReplaceCodesAsync without normalized and Guid id
	Given I have configured MySQL UserOnlyStore without normalized and Guid id
	And I created users for MySQL UserOnlyStore
		| UserName | Email            |
		| John     | john@example.com |
	When I execute "ReplaceCodesAsync" on MySQL UserOnlyStore with parameters
		| UserName | RecoveryCodes          |
		| John     | code-1;code-2;code-3  |
	Then the last integer result for MySQL UserOnlyStore should be 3

@xunit:collection[DatabaseWithGuidIdCollection]
Scenario: Redeem recovery code with RedeemCodeAsync without normalized and Guid id
	Given I have configured MySQL UserOnlyStore without normalized and Guid id
	And I created users for MySQL UserOnlyStore
		| UserName | Email            |
		| John     | john@example.com |
	And I replaced recovery codes for MySQL UserOnlyStore
		| UserName | RecoveryCodes          |
		| John     | code-1;code-2;code-3  |
	When I execute "RedeemCodeAsync" on MySQL UserOnlyStore with parameters
		| UserName | Code   |
		| John     | code-1 |
	Then the last boolean result for MySQL UserOnlyStore should be "True"
