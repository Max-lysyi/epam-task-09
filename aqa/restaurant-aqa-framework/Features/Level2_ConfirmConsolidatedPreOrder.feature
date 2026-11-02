@level2
Feature: Level 2 - Confirm Consolidated Pre-order (UI & API)
  As the Owner of a group reservation
  I want to submit the consolidated pre-order to the restaurant
  So that identical dishes are merged and sent for preparation

  @api @ui @smoke @happy_path
  Scenario: Owner submits consolidated pre-order merging identical dishes for restaurant
    Given an active group reservation exists with confirmed carts:
      | Name     | Type  | IsSubmitted | DishId | DishName     | Price | Quantity |
      | Yevhenii | Owner | true        | dish1  | Caesar Salad | 18.00 | 1        |
      | Olena    | Guest | true        | dish1  | Caesar Salad | 18.00 | 2        |
      | Olena    | Guest | true        | dish2  | Tom Yum      | 22.00 | 1        |
      | Andrii   | Guest | false       | dish3  | Spring Salad | 12.00 | 3        |
    When the Owner opens the group pre-order review page
    And the Owner submits the consolidated pre-order via UI
    Then the group status badge changes to "Sent to Restaurant"
    And the order status in restaurant backend is "SubmittedByCustomer"
    And the confirmed order composition has merged dishes:
      | DishId | Quantity |
      | dish1  | 3        |
      | dish2  | 1        |
    And the unconfirmed dish "dish3" is excluded from the order

  @api @negative
  Scenario: Cannot submit if no carts are confirmed
    Given an active group reservation exists where no participants have confirmed their cart:
      | Name   | Type  | IsSubmitted | DishId | DishName     | Price | Quantity |
      | Olena  | Guest | false       | dish1  | Caesar Salad | 18.00 | 1        |
      | Andrii | Guest | false       | dish2  | Tom Yum      | 22.00 | 2        |
    When the Owner submits the consolidated pre-order via API
    Then the API response status code is 400

  @api @negative
  Scenario: Cannot submit if confirmed carts are empty
    Given an active group reservation exists where confirmed participants have no dishes:
      | Name     | Type  | IsSubmitted |
      | Yevhenii | Owner | true        |
    When the Owner submits the consolidated pre-order via API
    Then the API response status code is 400

  @api @negative
  Scenario Outline: Cannot submit if group, reservation, or order does not exist
    When the Owner attempts to submit a pre-order with condition "<Condition>" via API
    Then the API response status code is <ExpectedStatusCode>

    Examples:
      | Condition                 | ExpectedStatusCode |
      | non-existent group        | 404                |
      | mismatched reservation id | 404                |
      | missing order in group    | 400                |

  @api @negative
  Scenario: Cannot submit a group that is already submitted
    Given a group reservation that has already been submitted
    When the Owner submits the consolidated pre-order via API
    Then the API response status code is 400

  @api @authorization
  Scenario Outline: Only the Owner can submit the consolidated pre-order
    Given an active group reservation with ready submitted carts exists
    When a user with role "<Role>" submits the consolidated pre-order via API
    Then the API response status code is <ExpectedStatusCode>

    Examples:
      | Role         | ExpectedStatusCode |
      | Owner        | 200                |
      | Guest        | 403                |
      | Waiter       | 403                |
      | OtherClient  | 403                |
      | Unauthorized | 401                |

