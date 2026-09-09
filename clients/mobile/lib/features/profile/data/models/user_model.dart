import 'dart:convert';

enum RoleType {
  employee, // 0
  team, // 1
  operator, // 2
}

class UserModel {
  final String id;
  final String firstName;
  final String lastName;
  final String email;
  final String phone;
  final String department;
  final RoleType roleType;
  final String? subRole;
  final String? avatarUrl;
  final String? googleEmail;

  const UserModel({
    required this.id,
    required this.firstName,
    required this.lastName,
    required this.email,
    required this.phone,
    required this.department,
    required this.roleType,
    this.subRole,
    this.avatarUrl,
    this.googleEmail,
  });

  String get fullName => '$firstName $lastName'.trim();
  bool get isGoogleLinked =>
      googleEmail != null && googleEmail!.trim().isNotEmpty;

  static RoleType parseRoleType(dynamic role) {
    if (role == null) return RoleType.employee;
    if (role is int) {
      if (role >= 0 && role < RoleType.values.length) {
        return RoleType.values[role];
      }
      return RoleType.employee;
    }
    final str = role.toString().trim().toLowerCase();
    if (str == 'team' || str == '1') return RoleType.team;
    if (str == 'operator' || str == '2') return RoleType.operator;
    return RoleType.employee;
  }

  factory UserModel.fromJson(Map<String, dynamic> json) {
    return UserModel(
      id: (json['id'] ?? json['Id'] ?? '').toString(),
      firstName: (json['firstName'] ?? json['FirstName'] ?? '').toString(),
      lastName: (json['lastName'] ?? json['LastName'] ?? '').toString(),
      email: (json['email'] ?? json['Email'] ?? '').toString(),
      phone: (json['phone'] ?? json['Phone'] ?? '').toString(),
      department: (json['department'] ?? json['Department'] ?? '').toString(),
      roleType: parseRoleType(json['roleType'] ?? json['RoleType']),
      subRole: json['subRole'] as String? ?? json['SubRole'] as String?,
      avatarUrl: json['avatarUrl'] as String? ?? json['AvatarUrl'] as String?,
      googleEmail:
          json['googleEmail'] as String? ?? json['GoogleEmail'] as String?,
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'firstName': firstName,
      'lastName': lastName,
      'email': email,
      'phone': phone,
      'department': department,
      'roleType': roleType.index,
      'subRole': subRole,
      'avatarUrl': avatarUrl,
      'googleEmail': googleEmail,
    };
  }

  String toRawJson() => json.encode(toJson());

  factory UserModel.fromRawJson(String str) =>
      UserModel.fromJson(json.decode(str) as Map<String, dynamic>);

  UserModel copyWith({
    String? id,
    String? firstName,
    String? lastName,
    String? email,
    String? phone,
    String? department,
    RoleType? roleType,
    String? subRole,
    String? avatarUrl,
    String? googleEmail,
  }) {
    return UserModel(
      id: id ?? this.id,
      firstName: firstName ?? this.firstName,
      lastName: lastName ?? this.lastName,
      email: email ?? this.email,
      phone: phone ?? this.phone,
      department: department ?? this.department,
      roleType: roleType ?? this.roleType,
      subRole: subRole ?? this.subRole,
      avatarUrl: avatarUrl ?? this.avatarUrl,
      googleEmail: googleEmail ?? this.googleEmail,
    );
  }
}
