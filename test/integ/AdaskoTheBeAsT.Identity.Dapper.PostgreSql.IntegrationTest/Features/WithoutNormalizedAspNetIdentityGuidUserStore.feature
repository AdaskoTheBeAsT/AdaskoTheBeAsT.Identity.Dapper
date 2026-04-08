Feature: WithoutNormalizedAspNetIdentityGuidUserStore

A short summary of the feature

@xunit:collection[DatabaseWithGuidIdCollection]
Scenario Outline: Verify lightweight UserStore method <method> without normalized and Guid id
    Given I have configured PostgreSQL UserStore without normalized and Guid id
    When I verify "<method>" on PostgreSQL UserStore without normalized and Guid id
    Then "<method>" on PostgreSQL UserStore should work without normalized and Guid id

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
    Given I have configured PostgreSQL UserStore without normalized and Guid id
    And I created users for PostgreSQL UserStore
        | UserName | Email             |
        | John     | john@example.com  |
    And I created roles for PostgreSQL UserStore
        | Name  |
        | Admin |
    And I added users to roles for PostgreSQL UserStore
        | UserName | RoleName |
        | John     | Admin    |
    When I execute "Users" on PostgreSQL UserStore
    Then the last users result for PostgreSQL UserStore should match
        | UserName | Email            |
        | John     | john@example.com |

@xunit:collection[DatabaseWithGuidIdCollection]
Scenario: Create user with CreateAsync without normalized and Guid id
    Given I have configured PostgreSQL UserStore without normalized and Guid id
    When I execute "CreateAsync" on PostgreSQL UserStore with parameters
        | UserName | Email            |
        | John     | john@example.com |
    Then the last identity result for PostgreSQL UserStore should be successful
    And the last user result for PostgreSQL UserStore should match
        | UserName | Email            |
        | John     | john@example.com |

@xunit:collection[DatabaseWithGuidIdCollection]
Scenario: Update user with UpdateAsync without normalized and Guid id
    Given I have configured PostgreSQL UserStore without normalized and Guid id
    And I created users for PostgreSQL UserStore
        | UserName | Email            |
        | John     | john@example.com |
    When I execute "UpdateAsync" on PostgreSQL UserStore with parameters
        | UserName | NewUserName | NewEmail              |
        | John     | Johnny      | johnny@example.com    |
    Then the last identity result for PostgreSQL UserStore should be successful
    And the last user result for PostgreSQL UserStore should match
        | UserName | Email               |
        | Johnny   | johnny@example.com  |

@xunit:collection[DatabaseWithGuidIdCollection]
Scenario: Delete user with DeleteAsync without normalized and Guid id
    Given I have configured PostgreSQL UserStore without normalized and Guid id
    And I created users for PostgreSQL UserStore
        | UserName | Email            |
        | John     | john@example.com |
    When I execute "DeleteAsync" on PostgreSQL UserStore with parameters
        | UserName |
        | John     |
    Then the last identity result for PostgreSQL UserStore should be successful
    And the last user result for PostgreSQL UserStore should be null

@xunit:collection[DatabaseWithGuidIdCollection]
Scenario: Find user with FindByIdAsync without normalized and Guid id
    Given I have configured PostgreSQL UserStore without normalized and Guid id
    And I created users for PostgreSQL UserStore
        | UserName | Email            |
        | John     | john@example.com |
    When I execute "FindByIdAsync" on PostgreSQL UserStore with parameters
        | UserName |
        | John     |
    Then the last user result for PostgreSQL UserStore should match
        | UserName | Email            |
        | John     | john@example.com |

@xunit:collection[DatabaseWithGuidIdCollection]
Scenario: Find user with FindByNameAsync without normalized and Guid id
    Given I have configured PostgreSQL UserStore without normalized and Guid id
    And I created users for PostgreSQL UserStore
        | UserName | Email            |
        | John     | john@example.com |
    When I execute "FindByNameAsync" on PostgreSQL UserStore with parameters
        | UserName |
        | John     |
    Then the last user result for PostgreSQL UserStore should match
        | UserName | Email            |
        | John     | john@example.com |

@xunit:collection[DatabaseWithGuidIdCollection]
Scenario: Find user with FindByEmailAsync without normalized and Guid id
    Given I have configured PostgreSQL UserStore without normalized and Guid id
    And I created users for PostgreSQL UserStore
        | UserName | Email            |
        | John     | john@example.com |
    When I execute "FindByEmailAsync" on PostgreSQL UserStore with parameters
        | Email            |
        | john@example.com |
    Then the last user result for PostgreSQL UserStore should match
        | UserName | Email            |
        | John     | john@example.com |

@xunit:collection[DatabaseWithGuidIdCollection]
Scenario: Get user claims with GetClaimsAsync without normalized and Guid id
    Given I have configured PostgreSQL UserStore without normalized and Guid id
    And I created users for PostgreSQL UserStore
        | UserName | Email            |
        | John     | john@example.com |
    And I added user claims for PostgreSQL UserStore
        | UserName | ClaimType  | ClaimValue |
        | John     | permission | read       |
        | John     | permission | write      |
    When I execute "GetClaimsAsync" on PostgreSQL UserStore with parameters
        | UserName |
        | John     |
    Then the last claims result for PostgreSQL UserStore should match
        | ClaimType  | ClaimValue |
        | permission | read       |
        | permission | write      |

@xunit:collection[DatabaseWithGuidIdCollection]
Scenario: Add user claims with AddClaimsAsync without normalized and Guid id
    Given I have configured PostgreSQL UserStore without normalized and Guid id
    And I created users for PostgreSQL UserStore
        | UserName | Email            |
        | John     | john@example.com |
    When I execute "AddClaimsAsync" on PostgreSQL UserStore with parameters
        | UserName | ClaimType  | ClaimValue |
        | John     | permission | read       |
        | John     | permission | write      |
    Then the last claims result for PostgreSQL UserStore should match
        | ClaimType  | ClaimValue |
        | permission | read       |
        | permission | write      |

@xunit:collection[DatabaseWithGuidIdCollection]
Scenario: Replace user claim with ReplaceClaimAsync without normalized and Guid id
    Given I have configured PostgreSQL UserStore without normalized and Guid id
    And I created users for PostgreSQL UserStore
        | UserName | Email            |
        | John     | john@example.com |
    And I added user claims for PostgreSQL UserStore
        | UserName | ClaimType  | ClaimValue |
        | John     | permission | read       |
    When I execute "ReplaceClaimAsync" on PostgreSQL UserStore with parameters
        | UserName | ClaimTypeOld | ClaimValueOld | ClaimTypeNew | ClaimValueNew |
        | John     | permission   | read          | permission   | write         |
    Then the last claims result for PostgreSQL UserStore should match
        | ClaimType  | ClaimValue |
        | permission | write      |

@xunit:collection[DatabaseWithGuidIdCollection]
Scenario: Remove user claims with RemoveClaimsAsync without normalized and Guid id
    Given I have configured PostgreSQL UserStore without normalized and Guid id
    And I created users for PostgreSQL UserStore
        | UserName | Email            |
        | John     | john@example.com |
    And I added user claims for PostgreSQL UserStore
        | UserName | ClaimType  | ClaimValue |
        | John     | permission | read       |
    When I execute "RemoveClaimsAsync" on PostgreSQL UserStore with parameters
        | UserName | ClaimType  | ClaimValue |
        | John     | permission | read       |
    Then the last claims result for PostgreSQL UserStore should match
        | ClaimType | ClaimValue |

@xunit:collection[DatabaseWithGuidIdCollection]
Scenario: Add user login with AddLoginAsync without normalized and Guid id
    Given I have configured PostgreSQL UserStore without normalized and Guid id
    And I created users for PostgreSQL UserStore
        | UserName | Email            |
        | John     | john@example.com |
    When I execute "AddLoginAsync" on PostgreSQL UserStore with parameters
        | UserName | LoginProvider | ProviderKey | ProviderDisplayName |
        | John     | GitHub        | john-gh     | GitHub              |
    Then the last logins result for PostgreSQL UserStore should match
        | LoginProvider | ProviderKey | ProviderDisplayName |
        | GitHub        | john-gh     | GitHub              |

@xunit:collection[DatabaseWithGuidIdCollection]
Scenario: Remove user login with RemoveLoginAsync without normalized and Guid id
    Given I have configured PostgreSQL UserStore without normalized and Guid id
    And I created users for PostgreSQL UserStore
        | UserName | Email            |
        | John     | john@example.com |
    And I added user logins for PostgreSQL UserStore
        | UserName | LoginProvider | ProviderKey | ProviderDisplayName |
        | John     | GitHub        | john-gh     | GitHub              |
    When I execute "RemoveLoginAsync" on PostgreSQL UserStore with parameters
        | UserName | LoginProvider | ProviderKey |
        | John     | GitHub        | john-gh     |
    Then the last logins result for PostgreSQL UserStore should match
        | LoginProvider | ProviderKey | ProviderDisplayName |

@xunit:collection[DatabaseWithGuidIdCollection]
Scenario: Get user logins with GetLoginsAsync without normalized and Guid id
    Given I have configured PostgreSQL UserStore without normalized and Guid id
    And I created users for PostgreSQL UserStore
        | UserName | Email            |
        | John     | john@example.com |
    And I added user logins for PostgreSQL UserStore
        | UserName | LoginProvider | ProviderKey | ProviderDisplayName |
        | John     | GitHub        | john-gh     | GitHub              |
    When I execute "GetLoginsAsync" on PostgreSQL UserStore with parameters
        | UserName |
        | John     |
    Then the last logins result for PostgreSQL UserStore should match
        | LoginProvider | ProviderKey | ProviderDisplayName |
        | GitHub        | john-gh     | GitHub              |

@xunit:collection[DatabaseWithGuidIdCollection]
Scenario: Find user by login with FindByLoginAsync without normalized and Guid id
    Given I have configured PostgreSQL UserStore without normalized and Guid id
    And I created users for PostgreSQL UserStore
        | UserName | Email            |
        | John     | john@example.com |
    And I added user logins for PostgreSQL UserStore
        | UserName | LoginProvider | ProviderKey | ProviderDisplayName |
        | John     | GitHub        | john-gh     | GitHub              |
    When I execute "FindByLoginAsync" on PostgreSQL UserStore with parameters
        | LoginProvider | ProviderKey |
        | GitHub        | john-gh     |
    Then the last user result for PostgreSQL UserStore should match
        | UserName | Email            |
        | John     | john@example.com |

@xunit:collection[DatabaseWithGuidIdCollection]
Scenario: Increment access failed count with IncrementAccessFailedCountAsync without normalized and Guid id
    Given I have configured PostgreSQL UserStore without normalized and Guid id
    And I created users for PostgreSQL UserStore
        | UserName | Email            | AccessFailedCount |
        | John     | john@example.com | 0                 |
    When I execute "IncrementAccessFailedCountAsync" on PostgreSQL UserStore with parameters
        | UserName |
        | John     |
    Then the last integer result for PostgreSQL UserStore should be 1
    And the last user result for PostgreSQL UserStore should match
        | UserName | AccessFailedCount |
        | John     | 1                 |

@xunit:collection[DatabaseWithGuidIdCollection]
Scenario: Get users for claim with GetUsersForClaimAsync without normalized and Guid id
    Given I have configured PostgreSQL UserStore without normalized and Guid id
    And I created users for PostgreSQL UserStore
        | UserName | Email            |
        | John     | john@example.com |
    And I added user claims for PostgreSQL UserStore
        | UserName | ClaimType  | ClaimValue |
        | John     | permission | read       |
    When I execute "GetUsersForClaimAsync" on PostgreSQL UserStore with parameters
        | ClaimType  | ClaimValue |
        | permission | read       |
    Then the last users result for PostgreSQL UserStore should match
        | UserName | Email            |
        | John     | john@example.com |

@xunit:collection[DatabaseWithGuidIdCollection]
Scenario: Set token with SetTokenAsync without normalized and Guid id
    Given I have configured PostgreSQL UserStore without normalized and Guid id
    And I created users for PostgreSQL UserStore
        | UserName | Email            |
        | John     | john@example.com |
    When I execute "SetTokenAsync" on PostgreSQL UserStore with parameters
        | UserName | LoginProvider | Name          | Value         |
        | John     | api           | refresh-token | refresh-value |
    Then the last string result for PostgreSQL UserStore should be "refresh-value"

@xunit:collection[DatabaseWithGuidIdCollection]
Scenario: Remove token with RemoveTokenAsync without normalized and Guid id
    Given I have configured PostgreSQL UserStore without normalized and Guid id
    And I created users for PostgreSQL UserStore
        | UserName | Email            |
        | John     | john@example.com |
    And I added user tokens for PostgreSQL UserStore
        | UserName | LoginProvider | Name          | Value         |
        | John     | api           | refresh-token | refresh-value |
    When I execute "RemoveTokenAsync" on PostgreSQL UserStore with parameters
        | UserName | LoginProvider | Name          |
        | John     | api           | refresh-token |
    Then the last string result for PostgreSQL UserStore should be null

@xunit:collection[DatabaseWithGuidIdCollection]
Scenario: Get token with GetTokenAsync without normalized and Guid id
    Given I have configured PostgreSQL UserStore without normalized and Guid id
    And I created users for PostgreSQL UserStore
        | UserName | Email            |
        | John     | john@example.com |
    And I added user tokens for PostgreSQL UserStore
        | UserName | LoginProvider | Name          | Value         |
        | John     | api           | refresh-token | refresh-value |
    When I execute "GetTokenAsync" on PostgreSQL UserStore with parameters
        | UserName | LoginProvider | Name          |
        | John     | api           | refresh-token |
    Then the last string result for PostgreSQL UserStore should be "refresh-value"

@xunit:collection[DatabaseWithGuidIdCollection]
Scenario: Set authenticator key with SetAuthenticatorKeyAsync without normalized and Guid id
    Given I have configured PostgreSQL UserStore without normalized and Guid id
    And I created users for PostgreSQL UserStore
        | UserName | Email            |
        | John     | john@example.com |
    When I execute "SetAuthenticatorKeyAsync" on PostgreSQL UserStore with parameters
        | UserName | Key      |
        | John     | auth-key |
    Then the last string result for PostgreSQL UserStore should be "auth-key"

@xunit:collection[DatabaseWithGuidIdCollection]
Scenario: Get authenticator key with GetAuthenticatorKeyAsync without normalized and Guid id
    Given I have configured PostgreSQL UserStore without normalized and Guid id
    And I created users for PostgreSQL UserStore
        | UserName | Email            |
        | John     | john@example.com |
    And I set authenticator keys for PostgreSQL UserStore
        | UserName | Key      |
        | John     | auth-key |
    When I execute "GetAuthenticatorKeyAsync" on PostgreSQL UserStore with parameters
        | UserName |
        | John     |
    Then the last string result for PostgreSQL UserStore should be "auth-key"

@xunit:collection[DatabaseWithGuidIdCollection]
Scenario: Count recovery codes with CountCodesAsync without normalized and Guid id
    Given I have configured PostgreSQL UserStore without normalized and Guid id
    And I created users for PostgreSQL UserStore
        | UserName | Email            |
        | John     | john@example.com |
    And I replaced recovery codes for PostgreSQL UserStore
        | UserName | RecoveryCodes          |
        | John     | code-1;code-2;code-3  |
    When I execute "CountCodesAsync" on PostgreSQL UserStore with parameters
        | UserName |
        | John     |
    Then the last integer result for PostgreSQL UserStore should be 3

@xunit:collection[DatabaseWithGuidIdCollection]
Scenario: Replace recovery codes with ReplaceCodesAsync without normalized and Guid id
    Given I have configured PostgreSQL UserStore without normalized and Guid id
    And I created users for PostgreSQL UserStore
        | UserName | Email            |
        | John     | john@example.com |
    When I execute "ReplaceCodesAsync" on PostgreSQL UserStore with parameters
        | UserName | RecoveryCodes          |
        | John     | code-1;code-2;code-3  |
    Then the last integer result for PostgreSQL UserStore should be 3

@xunit:collection[DatabaseWithGuidIdCollection]
Scenario: Redeem recovery code with RedeemCodeAsync without normalized and Guid id
    Given I have configured PostgreSQL UserStore without normalized and Guid id
    And I created users for PostgreSQL UserStore
        | UserName | Email            |
        | John     | john@example.com |
    And I replaced recovery codes for PostgreSQL UserStore
        | UserName | RecoveryCodes          |
        | John     | code-1;code-2;code-3  |
    When I execute "RedeemCodeAsync" on PostgreSQL UserStore with parameters
        | UserName | Code   |
        | John     | code-1 |
    Then the last boolean result for PostgreSQL UserStore should be "True"

@xunit:collection[DatabaseWithGuidIdCollection]
Scenario: Get users in role with GetUsersInRoleAsync without normalized and Guid id
    Given I have configured PostgreSQL UserStore without normalized and Guid id
    And I created users for PostgreSQL UserStore
        | UserName | Email            |
        | John     | john@example.com |
    And I created roles for PostgreSQL UserStore
        | Name  |
        | Admin |
    And I added users to roles for PostgreSQL UserStore
        | UserName | RoleName |
        | John     | Admin    |
    When I execute "GetUsersInRoleAsync" on PostgreSQL UserStore with parameters
        | RoleName |
        | Admin    |
    Then the last users result for PostgreSQL UserStore should match
        | UserName | Email            |
        | John     | john@example.com |

@xunit:collection[DatabaseWithGuidIdCollection]
Scenario: Add user to role with AddToRoleAsync without normalized and Guid id
    Given I have configured PostgreSQL UserStore without normalized and Guid id
    And I created users for PostgreSQL UserStore
        | UserName | Email            |
        | John     | john@example.com |
    And I created roles for PostgreSQL UserStore
        | Name  |
        | Admin |
    When I execute "AddToRoleAsync" on PostgreSQL UserStore with parameters
        | UserName | RoleName |
        | John     | Admin    |
    Then the last strings result for PostgreSQL UserStore should match
        | Name  |
        | Admin |

@xunit:collection[DatabaseWithGuidIdCollection]
Scenario: Remove user from role with RemoveFromRoleAsync without normalized and Guid id
    Given I have configured PostgreSQL UserStore without normalized and Guid id
    And I created users for PostgreSQL UserStore
        | UserName | Email            |
        | John     | john@example.com |
    And I created roles for PostgreSQL UserStore
        | Name  |
        | Admin |
    And I added users to roles for PostgreSQL UserStore
        | UserName | RoleName |
        | John     | Admin    |
    When I execute "RemoveFromRoleAsync" on PostgreSQL UserStore with parameters
        | UserName | RoleName |
        | John     | Admin    |
    Then the last strings result for PostgreSQL UserStore should match
        | Name |

@xunit:collection[DatabaseWithGuidIdCollection]
Scenario: Get roles for user with GetRolesAsync without normalized and Guid id
    Given I have configured PostgreSQL UserStore without normalized and Guid id
    And I created users for PostgreSQL UserStore
        | UserName | Email            |
        | John     | john@example.com |
    And I created roles for PostgreSQL UserStore
        | Name  |
        | Admin |
    And I added users to roles for PostgreSQL UserStore
        | UserName | RoleName |
        | John     | Admin    |
    When I execute "GetRolesAsync" on PostgreSQL UserStore with parameters
        | UserName |
        | John     |
    Then the last strings result for PostgreSQL UserStore should match
        | Name  |
        | Admin |

@xunit:collection[DatabaseWithGuidIdCollection]
Scenario: Check role membership with IsInRoleAsync without normalized and Guid id
    Given I have configured PostgreSQL UserStore without normalized and Guid id
    And I created users for PostgreSQL UserStore
        | UserName | Email            |
        | John     | john@example.com |
    And I created roles for PostgreSQL UserStore
        | Name  |
        | Admin |
    And I added users to roles for PostgreSQL UserStore
        | UserName | RoleName |
        | John     | Admin    |
    When I execute "IsInRoleAsync" on PostgreSQL UserStore with parameters
        | UserName | RoleName |
        | John     | Admin    |
    Then the last boolean result for PostgreSQL UserStore should be "True"

@xunit:collection[DatabaseWithGuidIdCollection]
Scenario: Get role claims for user with GetRoleClaimsAsync without normalized and Guid id
    Given I have configured PostgreSQL UserStore without normalized and Guid id
    And I created users for PostgreSQL UserStore
        | UserName | Email            |
        | John     | john@example.com |
    And I created roles for PostgreSQL UserStore
        | Name  |
        | Admin |
    And I added role claims for PostgreSQL UserStore
        | RoleName | ClaimType  | ClaimValue   |
        | Admin    | permission | manage-users |
    And I added users to roles for PostgreSQL UserStore
        | UserName | RoleName |
        | John     | Admin    |
    When I execute "GetRoleClaimsAsync" on PostgreSQL UserStore with parameters
        | UserName |
        | John     |
    Then the last claims result for PostgreSQL UserStore should match
        | ClaimType  | ClaimValue   |
        | permission | manage-users |

@xunit:collection[DatabaseWithGuidIdCollection]
Scenario: Get user and role claims with GetUserAndRoleClaimsAsync without normalized and Guid id
    Given I have configured PostgreSQL UserStore without normalized and Guid id
    And I created users for PostgreSQL UserStore
        | UserName | Email            |
        | John     | john@example.com |
    And I created roles for PostgreSQL UserStore
        | Name  |
        | Admin |
    And I added user claims for PostgreSQL UserStore
        | UserName | ClaimType  | ClaimValue |
        | John     | department | sales      |
    And I added role claims for PostgreSQL UserStore
        | RoleName | ClaimType  | ClaimValue   |
        | Admin    | permission | manage-users |
    And I added users to roles for PostgreSQL UserStore
        | UserName | RoleName |
        | John     | Admin    |
    When I execute "GetUserAndRoleClaimsAsync" on PostgreSQL UserStore with parameters
        | UserName |
        | John     |
    Then the last claims result for PostgreSQL UserStore should match
        | ClaimType  | ClaimValue   |
        | department | sales        |
        | permission | manage-users |

